using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using Newtonsoft.Json; // Cài NuGet Newtonsoft.Json nếu chưa có

namespace PhoneStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Xem giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        // 2. Thêm vào giỏ
        public async Task<IActionResult> AddToCart(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item == null)
            {
                cart.Add(new OrderDetail { ProductId = productId, Product = product, Quantity = 1, Price = product.Price });
            }
            else
            {
                item.Quantity++;
            }
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // 3. Thanh toán (Phải đăng nhập)
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (cart.Count == 0) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);
            var order = new Order
            {
                UserId = user.Id,
                CustomerName = user.FullName ?? user.UserName,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(i => i.Price * i.Quantity),
                Status = "Pending",
                BranchId = 1, // Mặc định gán cho chi nhánh chính
                OrderDetails = cart
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Xóa giỏ hàng sau khi đặt
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("History", "Order");
        }

        // Các hàm phụ trợ lưu Session
        private List<OrderDetail> GetCartItems()
        {
            var sessionCart = HttpContext.Session.GetString("Cart");
            return sessionCart != null ? JsonConvert.DeserializeObject<List<OrderDetail>>(sessionCart) : new List<OrderDetail>();
        }
        private void SaveCart(List<OrderDetail> cart) => HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
    }
}