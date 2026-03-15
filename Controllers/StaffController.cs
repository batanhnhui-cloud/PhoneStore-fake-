using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data; // Thay bằng namespace DbContext của bạn

namespace PhoneStore.Controllers
{
    [Authorize(Roles = "Admin,Staff")] // Admin và Nhân viên đều vào được
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách đơn hàng cần xử lý
        public async Task<IActionResult> OrderManagement()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // Cập nhật trạng thái đơn hàng nhanh
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status; // Ví dụ: "Đã xác nhận", "Đang giao", "Thành công"
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(OrderManagement));
        }
    }
}