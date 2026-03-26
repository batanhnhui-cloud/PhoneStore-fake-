using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    // Tạo một ViewModel nhỏ để chứa dữ liệu Thống kê kho
    public class InventoryStatVM
    {
        public string BranchName { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int AvailableCount { get; set; }
    }

    [Authorize(Roles = "Staff, Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. GIAO DIỆN TỔNG QUAN KHO (INVENTORY)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            // Nhóm và đếm số lượng máy đang "Available" (Sẵn sàng bán) theo từng Sản phẩm và Chi nhánh
            var stats = await _context.DeviceImeis
                .Include(d => d.Product)
                .Include(d => d.Branch)
                .Where(d => d.Status == "Available")
                .GroupBy(d => new { d.BranchId, BranchName = d.Branch!.Name, d.ProductId, ProductName = d.Product!.Name })
                .Select(g => new InventoryStatVM
                {
                    BranchName = g.Key.BranchName,
                    ProductName = g.Key.ProductName,
                    AvailableCount = g.Count()
                })
                .ToListAsync();

            return View(stats);
        }

        // ==========================================
        // 2. NHẬP KHO BẰNG CÁCH QUÉT MÃ IMEI
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ImportImei()
        {
            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportImei(int branchId, int productId, string imeiList)
        {
            if (string.IsNullOrWhiteSpace(imeiList))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập ít nhất 1 mã IMEI!";
                return RedirectToAction(nameof(ImportImei));
            }

            var imeis = imeiList.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(i => i.Trim())
                                .Distinct()
                                .ToList();

            int successCount = 0;
            int duplicateCount = 0;

            foreach (var imei in imeis)
            {
                if (imei.Length != 15 || !imei.All(char.IsDigit)) continue; // Bỏ qua nếu không đúng chuẩn 15 số

                bool exists = await _context.DeviceImeis.AnyAsync(d => d.Imei == imei);
                if (exists)
                {
                    duplicateCount++;
                    continue; // Trùng lặp thì bỏ qua
                }

                _context.DeviceImeis.Add(new DeviceImei
                {
                    Imei = imei,
                    ProductId = productId,
                    BranchId = branchId,
                    Status = "Available"
                });
                successCount++;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã nhập kho thành công {successCount} máy. Bỏ qua {duplicateCount} mã lỗi/trùng lặp.";
            return RedirectToAction(nameof(Inventory)); // Nhập xong quay về xem Tồn kho
        }

        // ==========================================
        // 3. DANH SÁCH ĐƠN HÀNG ONLINE CẦN XỬ LÝ
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> OrderManagement()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Branch)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ==========================================
        // 4. QUÉT MÃ IMEI ĐỂ XUẤT KHO & CHỐT ĐƠN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> FulfillOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || order.Status != "Pending") return RedirectToAction(nameof(OrderManagement));
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> FulfillOrder(int orderId, List<string> scannedImeis)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            scannedImeis = scannedImeis.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            int totalQuantityRequired = order.OrderDetails.Sum(od => od.Quantity);

            if (scannedImeis.Count != totalQuantityRequired)
            {
                TempData["ErrorMessage"] = $"Lỗi: Đơn này cần giao {totalQuantityRequired} máy, nhưng bạn mới quét {scannedImeis.Count} mã.";
                return RedirectToAction(nameof(FulfillOrder), new { id = orderId });
            }

            foreach (var imei in scannedImeis)
            {
                var device = await _context.DeviceImeis
                    .FirstOrDefaultAsync(d => d.Imei == imei && d.Status == "Available");

                if (device == null)
                {
                    TempData["ErrorMessage"] = $"Lỗi: Máy có mã [{imei}] không hợp lệ hoặc đã bị bán cho người khác!";
                    return RedirectToAction(nameof(FulfillOrder), new { id = orderId });
                }

                // Gắn máy cho đơn hàng và kích hoạt bảo hành
                device.Status = "Sold";
                device.OrderId = order.Id;
                device.WarrantyActivationDate = DateTime.Now;
                device.WarrantyExpirationDate = DateTime.Now.AddMonths(12);
            }

            order.Status = "Success"; // Đổi trạng thái đơn thành Thành công
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Đã xuất kho thành công Đơn hàng #{orderId}!";
            return RedirectToAction(nameof(OrderManagement));
        }

        // ==========================================
        // 5. HỦY ĐƠN HÀNG HOẶC ĐỔI TRẠNG THÁI KHÁC
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            // Lấy đơn hàng bao gồm cả các máy (IMEI) đã xuất cho đơn này
            var order = await _context.Orders
                .Include(o => o.DeviceImeis)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                order.Status = status;

                // TÍNH NĂNG ĐỈNH CAO: Nếu đơn hàng bị Hủy (Cancelled), tự động nhả lại các máy về Kho
                if (status == "Cancelled" && order.DeviceImeis != null)
                {
                    foreach (var device in order.DeviceImeis)
                    {
                        device.Status = "Available"; // Sẵn sàng bán lại
                        device.OrderId = null; // Gỡ máy khỏi đơn hàng
                        device.WarrantyActivationDate = null; // Hủy kích hoạt bảo hành
                        device.WarrantyExpirationDate = null;
                    }
                    TempData["SuccessMessage"] = $"Đã hủy đơn #{id} và tự động nhập lại các máy vào kho thành công!";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn #{id} thành công!";
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(OrderManagement));
        }

        // ==========================================
        // 6. BÁN HÀNG TẠI QUẦY (POS - QUÉT IMEI TẠO HÓA ĐƠN)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> POS()
        {
            // Lấy danh sách Chi nhánh cho nhân viên chọn quầy họ đang đứng
            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> POS(string customerName, string phone, int branchId, string scannedImeis)
        {
            if (string.IsNullOrWhiteSpace(scannedImeis))
            {
                TempData["ErrorMessage"] = "Vui lòng quét ít nhất 1 mã IMEI để bán!";
                return RedirectToAction(nameof(POS));
            }

            // Tách các mã IMEI vừa quét
            var imeis = scannedImeis.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(i => i.Trim()).Distinct().ToList();

            // Tìm các máy trong Database khớp với mã IMEI vừa quét (Phải đang Available và đúng Chi nhánh)
            var validDevices = await _context.DeviceImeis
                .Include(d => d.Product)
                .Where(d => imeis.Contains(d.Imei) && d.Status == "Available" && d.BranchId == branchId)
                .ToListAsync();

            // Nếu số máy tìm thấy không khớp với số mã quét (tức là có mã sai/mã ảo)
            if (validDevices.Count != imeis.Count)
            {
                TempData["ErrorMessage"] = "Cảnh báo: Có mã IMEI không tồn tại, đã bán, hoặc không nằm trong kho của Chi nhánh này!";
                return RedirectToAction(nameof(POS));
            }

            // 1. TẠO ĐƠN HÀNG MỚI (Trạng thái: Success - Vì khách mua trực tiếp trả tiền luôn)
            var newOrder = new Order
            {
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Khách lẻ" : customerName,
                Phone = phone ?? "",
                Address = "Mua trực tiếp tại quầy",
                OrderDate = DateTime.Now,
                Status = "Success",
                BranchId = branchId,
                TotalAmount = validDevices.Sum(d => d.Product!.Price) // Cộng tổng tiền các máy đã quét
            };
            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync(); // Lưu để lấy ID đơn hàng

            // 2. TẠO CHI TIẾT ĐƠN HÀNG (Gom nhóm nếu khách mua 2 cái cùng loại)
            var productGroups = validDevices.GroupBy(d => d.ProductId);
            foreach (var group in productGroups)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = newOrder.Id,
                    ProductId = group.Key,
                    Quantity = group.Count(),
                    Price = group.First().Product!.Price
                });
            }

            // 3. CẬP NHẬT TRẠNG THÁI IMEI VÀ KÍCH HOẠT BẢO HÀNH
            foreach (var device in validDevices)
            {
                device.Status = "Sold";
                device.OrderId = newOrder.Id;
                device.WarrantyActivationDate = DateTime.Now;
                device.WarrantyExpirationDate = DateTime.Now.AddMonths(12);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ TING TING! Đã tạo hóa đơn #{newOrder.Id} thành công. Tổng thu: {newOrder.TotalAmount.ToString("N0")}đ.";
            return RedirectToAction(nameof(POS));
        }

        // ==========================================
        // 7. LUÂN CHUYỂN HÀNG GIỮA CÁC CHI NHÁNH
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> TransferStock()
        {
            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");

            // Lấy danh sách các phiếu luân chuyển đang "Đi trên đường" (Pending)
            var pendingTransfers = await _context.ImeiTransfers
                .Include(t => t.DeviceImei)
                .ThenInclude(d => d.Product)
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Where(t => t.Status == "Pending")
                .OrderByDescending(t => t.TransferDate)
                .ToListAsync();

            return View(pendingTransfers);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransfer(int fromBranchId, int toBranchId, string scannedImeis)
        {
            if (fromBranchId == toBranchId)
            {
                TempData["ErrorMessage"] = "Lỗi: Chi nhánh Xuất và Chi nhánh Nhận không được trùng nhau!";
                return RedirectToAction(nameof(TransferStock));
            }

            if (string.IsNullOrWhiteSpace(scannedImeis))
            {
                TempData["ErrorMessage"] = "Vui lòng quét ít nhất 1 mã IMEI để chuyển!";
                return RedirectToAction(nameof(TransferStock));
            }

            var imeis = scannedImeis.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(i => i.Trim()).Distinct().ToList();

            // Tìm máy trong kho Xuất (Phải đang Available)
            var validDevices = await _context.DeviceImeis
                .Where(d => imeis.Contains(d.Imei) && d.Status == "Available" && d.BranchId == fromBranchId)
                .ToListAsync();

            if (validDevices.Count != imeis.Count)
            {
                TempData["ErrorMessage"] = "Lỗi: Có mã IMEI không hợp lệ, không nằm ở kho xuất, hoặc đã bị bán mất rồi!";
                return RedirectToAction(nameof(TransferStock));
            }

            foreach (var device in validDevices)
            {
                device.Status = "Transferring"; // Đóng băng máy: Đang đi trên đường, không được phép bán!

                _context.ImeiTransfers.Add(new ImeiTransfer
                {
                    DeviceImeiId = device.Id,
                    FromBranchId = fromBranchId,
                    ToBranchId = toBranchId,
                    TransferDate = DateTime.Now,
                    Status = "Pending" // Đang chờ chi nhánh kia nhận
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"✅ Đã tạo phiếu xuất kho luân chuyển {validDevices.Count} máy thành công! Đang chờ chi nhánh nhận xác nhận.";
            return RedirectToAction(nameof(TransferStock));
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveTransfer(int transferId)
        {
            var transfer = await _context.ImeiTransfers
                .Include(t => t.DeviceImei)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer != null && transfer.Status == "Pending")
            {
                transfer.Status = "Completed"; // Phiếu hoàn tất
                transfer.ReceiveDate = DateTime.Now; // Chốt giờ nhận

                if (transfer.DeviceImei != null)
                {
                    transfer.DeviceImei.BranchId = transfer.ToBranchId; // Đổi hộ khẩu máy sang chi nhánh mới
                    transfer.DeviceImei.Status = "Available"; // Rã đông: Sẵn sàng bán tại chi nhánh mới
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"✅ Đã NHẬN HÀNG thành công! Mã IMEI: {transfer.DeviceImei?.Imei} đã được đưa vào kho của bạn.";
            }
            return RedirectToAction(nameof(TransferStock));
        }

    }
}