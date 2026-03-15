using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    [Authorize] // Phải đăng nhập mới vào được
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context; _userManager = userManager;
        }

        // Xem lịch sử mua hàng
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Xem chi tiết một đơn hàng
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}