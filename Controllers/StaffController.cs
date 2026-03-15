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
    }
}