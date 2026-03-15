using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    [Authorize(Roles = "Staff,Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Chỉ hiển thị đơn hàng thuộc chi nhánh của nhân viên đang đăng nhập
        public async Task<IActionResult> MyBranchOrders()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Nếu là Admin thì thấy hết, nếu là Staff thì lọc theo BranchId
            var query = _context.Orders.AsQueryable();
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(o => o.BranchId == currentUser.BranchId);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int productId, int quantityChange)
        {
            var user = await _userManager.GetUserAsync(User);
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.BranchId == user.BranchId);

            if (inventory != null)
            {
                inventory.StockQuantity += quantityChange;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}