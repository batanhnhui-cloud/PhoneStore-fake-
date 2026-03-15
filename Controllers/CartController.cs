using Microsoft.AspNetCore.Mvc;
using PhoneStore.Data;
using PhoneStore.Models;
using PhoneStore.Helpers;

namespace PhoneStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng hiện tại từ Session
        public List<CartItem> GetCartItems()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("GioHang");
            return cart ?? new List<CartItem>();
        }

        // 1. Trang hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCartItems();
            ViewBag.Total = cart.Sum(s => s.Total);
            return View(cart);
        }

        // 2. Thêm sản phẩm vào giỏ
        public IActionResult AddToCart(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var item = cart.Find(p => p.ProductId == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    Image = product.ImageUrl ?? ""
                });
            }
            else
            {
                item.Quantity++;
            }

            HttpContext.Session.Set("GioHang", cart);
            return RedirectToAction("Index");
        }

        // 3. Xóa sản phẩm khỏi giỏ
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCartItems();
            var item = cart.Find(p => p.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
            }
            HttpContext.Session.Set("GioHang", cart);
            return RedirectToAction("Index");
        }
    }
}