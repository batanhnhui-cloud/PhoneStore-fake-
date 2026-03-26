using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PhoneStore.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context; _userManager = userManager;
        }

        public async Task<IActionResult> OrderManagement()
        {
            var user = await _userManager.GetUserAsync(User);
            // Include thêm OrderDetails và Product để hiện tên máy trong đơn hàng
            var query = _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .AsQueryable();

            if (!User.IsInRole("Admin") && user.BranchId.HasValue)
                query = query.Where(o => o.BranchId == user.BranchId);

            return View(await query.OrderByDescending(o => o.OrderDate).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null) { order.Status = status; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(OrderManagement));
        }

        public async Task<IActionResult> Inventory()
        {
            var user = await _userManager.GetUserAsync(User);
            var query = _context.Inventories.Include(i => i.Product).Include(i => i.Branch).AsQueryable();
            if (!User.IsInRole("Admin") && user.BranchId.HasValue) query = query.Where(i => i.BranchId == user.BranchId);
            return View(await query.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> QuickUpdateStock(int productId, int branchId, int adjustment)
        {
            var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == branchId);
            if (inv != null) { inv.StockQuantity += adjustment; if (inv.StockQuantity < 0) inv.StockQuantity = 0; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Inventory));
        }

        // ==========================================
        // TÍNH NĂNG BÁN HÀNG TẠI QUẦY (POS)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> POS()
        {
            var user = await _userManager.GetUserAsync(User);

            // Chỉ lấy các sản phẩm CÒN HÀNG tại chi nhánh của nhân viên này
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.BranchId == user.BranchId && i.StockQuantity > 0)
                .ToListAsync();

            ViewBag.Inventory = inventory;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutPOS(string customerName, string phone, int productId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            var product = await _context.Products.FindAsync(productId);
            var inv = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == user.BranchId);

            // 1. Kiểm tra kho
            if (inv == null || inv.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = "Lỗi: Số lượng trong kho không đủ để bán!";
                return RedirectToAction(nameof(POS));
            }

            // 2. Trừ tồn kho ngay lập tức
            inv.StockQuantity -= quantity;

            // 3. Tạo hóa đơn (Ghi nhận nhân viên nào đã bán để tính KPI sau này)
            var order = new Order
            {
                UserId = user.Id, // Lấy ID của nhân viên đang thao tác
                CustomerName = customerName + " (Khách mua tại quầy)",
                Phone = phone,
                Address = "Mua trực tiếp tại chi nhánh",
                BranchId = user.BranchId,
                TotalAmount = product.Price * quantity,
                Status = "Success", // Đơn tại quầy mặc định là thành công
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Lưu để lấy ID Đơn hàng

            // 4. Lưu chi tiết máy khách mua
            var detail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = productId,
                Quantity = quantity,
                Price = product.Price
            };

            _context.OrderDetails.Add(detail);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chốt đơn thành công cho khách {customerName}!";
            return RedirectToAction(nameof(OrderManagement));
        }

        // 1. HIỂN THỊ FORM NHẬP KHO IMEI
        [HttpGet]
        public async Task<IActionResult> ImportImei()
        {
            // Lấy danh sách Chi nhánh và Điện thoại từ Database truyền sang Giao diện
            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "Id", "Name");
            return View();
        }

        // 2. XỬ LÝ LƯU HÀNG LOẠT IMEI VÀO DATABASE
        [HttpPost]
        public async Task<IActionResult> ImportImei(int branchId, int productId, string imeiList)
        {
            if (string.IsNullOrWhiteSpace(imeiList))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập ít nhất 1 mã IMEI!";
                return RedirectToAction(nameof(ImportImei));
            }

            // Tách chuỗi dữ liệu thành từng dòng (hỗ trợ cả dấu xuống dòng và dấu phẩy)
            var imeis = imeiList.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(i => i.Trim())
                                .Distinct() // Loại bỏ các mã nhập trùng lặp trong cùng 1 lần nhập
                                .ToList();

            int successCount = 0;
            int duplicateCount = 0;

            foreach (var imei in imeis)
            {
                // Kiểm tra sơ bộ: IMEI phải đủ 15 số
                if (imei.Length != 15 || !imei.All(char.IsDigit)) continue;

                // Kiểm tra xem mã này đã từng tồn tại trong Database chưa (Chống trùng lặp toàn hệ thống)
                bool exists = await _context.DeviceImeis.AnyAsync(d => d.Imei == imei);
                if (exists)
                {
                    duplicateCount++;
                    continue;
                }

                // Nếu hợp lệ, tạo máy mới và đưa vào kho
                var newDevice = new DeviceImei
                {
                    Imei = imei,
                    ProductId = productId,
                    BranchId = branchId,
                    Status = "Available" // Trạng thái: Sẵn sàng bán
                };
                _context.DeviceImeis.Add(newDevice);
                successCount++;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Đã nhập kho thành công {successCount} thiết bị mới. Bỏ qua {duplicateCount} mã lỗi/trùng lặp.";
            return RedirectToAction(nameof(ImportImei));
        }

        // 3. MỞ GIAO DIỆN QUÉT IMEI ĐỂ XUẤT BÁN (XỬ LÝ ĐƠN HÀNG)
        [HttpGet]
        public async Task<IActionResult> FulfillOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Branch)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || order.Status != "Pending") return RedirectToAction(nameof(OrderManagement));
            return View(order);
        }

        // 4. KIỂM TRA MÃ IMEI VÀ CHỐT ĐƠN (KÍCH HOẠT BẢO HÀNH)
        [HttpPost]
        public async Task<IActionResult> FulfillOrder(int orderId, List<string> scannedImeis)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            // Lọc bỏ các ô nhập rỗng
            scannedImeis = scannedImeis.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();

            // Đếm tổng số lượng máy mà khách đặt trong đơn này
            int totalQuantityRequired = order.OrderDetails.Sum(od => od.Quantity);

            if (scannedImeis.Count != totalQuantityRequired)
            {
                TempData["ErrorMessage"] = $"Lỗi: Khách đặt mua {totalQuantityRequired} máy, nhưng bạn mới quét {scannedImeis.Count} mã IMEI.";
                return RedirectToAction(nameof(FulfillOrder), new { id = orderId });
            }

            // Xử lý kiểm tra từng mã IMEI được quét
            foreach (var imei in scannedImeis)
            {
                // Tìm máy trong kho: Phải khớp IMEI, Phải đang "Available", và Phải nằm đúng ở Chi nhánh đang xử lý đơn
                var device = await _context.DeviceImeis
                    .FirstOrDefaultAsync(d => d.Imei == imei && d.Status == "Available" && d.BranchId == order.BranchId);

                if (device == null)
                {
                    TempData["ErrorMessage"] = $"Lỗi: Mã IMEI [{imei}] không hợp lệ! (Có thể do nhập sai, máy không nằm ở chi nhánh này, hoặc máy đã bị bán).";
                    return RedirectToAction(nameof(FulfillOrder), new { id = orderId });
                }

                // CẬP NHẬT TRẠNG THÁI MÁY & KÍCH HOẠT BẢO HÀNH
                device.Status = "Sold"; // Đổi thành Đã bán
                device.OrderId = order.Id; // Gắn vào đơn hàng này
                device.WarrantyActivationDate = DateTime.Now; // Ngày kích hoạt là NGAY BÂY GIỜ
                device.WarrantyExpirationDate = DateTime.Now.AddMonths(12); // Hạn bảo hành +12 tháng
            }

            // Cập nhật đơn hàng thành công
            order.Status = "Success";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Chốt đơn #{orderId} thành công! Các thiết bị đã được trừ kho và kích hoạt bảo hành điện tử.";
            return RedirectToAction(nameof(OrderManagement));
        }

    }
}