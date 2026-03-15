using Microsoft.AspNetCore.Mvc;
using PhoneStore.Data;
using PhoneStore.Models;
using PhoneStore.Helpers;
using Microsoft.EntityFrameworkCore;

namespace PhoneStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang điền thông tin khách hàng (GET)
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang");
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }
            return View();
        }

        // 2. Xử lý lưu đơn hàng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang");

            if (cart != null && cart.Any())
            {
                // Gán các thông tin tự động
                order.OrderDate = DateTime.Now;
                order.TotalAmount = cart.Sum(i => i.Total);

                // Lưu thông tin đơn hàng chung vào bảng Orders
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Lưu để lấy được order.Id vừa tự sinh

                // Lưu từng sản phẩm trong giỏ vào bảng OrderDetails
                foreach (var item in cart)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderDetails.Add(detail);
                }
                await _context.SaveChangesAsync();

                // Xóa giỏ hàng sau khi đặt thành công
                HttpContext.Session.Remove("GioHang");

                return RedirectToAction("Success");
            }

            return View(order);
        }

        public IActionResult Success()
        {
            return View();
        }

        // 3. Trang dành cho ADMIN: Xem danh sách đơn hàng đã nhận
        public async Task<IActionResult> AdminIndex()
        {
            var orders = await _context.Orders.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        // Thêm 2 hàm này vào file OrderController.cs hiện tại của bạn
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminIndex));
        }

    }
}