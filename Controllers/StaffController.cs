using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

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

    }
}