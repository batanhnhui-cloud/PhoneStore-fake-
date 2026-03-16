using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

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

        // 1. XEM GIỎ HÀNG
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        // 2. THÊM VÀO GIỎ
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existingItem != null) { existingItem.Quantity += quantity; }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            TempData["SuccessMessage"] = "Đã thêm " + product.Name + " vào giỏ hàng!";
            return RedirectToAction("Index", "Home");
        }

        // 3. XÓA KHỎI GIỎ
        public IActionResult RemoveFromCart(int id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");
            if (cart != null)
            {
                cart.RemoveAll(c => c.ProductId == id);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. TRANG THANH TOÁN (GET)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any()) return RedirectToAction(nameof(Index));

            // Nếu khách ĐÃ đăng nhập, tự điền sẵn tên và số điện thoại
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.CustomerName = user.FullName;
                ViewBag.Phone = user.PhoneNumber;
            }

            return View(cart);
        }

        // 5. CHỐT ĐƠN HÀNG (POST)
        [HttpPost]
        public async Task<IActionResult> Checkout(string customerName, string phone, string address)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");
            if (cart == null || !cart.Any()) return RedirectToAction(nameof(Index));

            var user = await _userManager.GetUserAsync(User);
            var defaultBranch = await _context.Branches.FirstOrDefaultAsync(); // Gán tạm đơn cho chi nhánh đầu tiên xử lý

            var order = new Order
            {
                CustomerName = customerName,
                Phone = phone,
                Address = address,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(c => c.Total),
                Status = "Pending",
                BranchId = defaultBranch?.Id,
                UserId = user?.Id // Nếu đăng nhập thì có ID, nếu khách vãng lai thì tự động = Null
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                _context.OrderDetails.Add(new OrderDetail { OrderId = order.Id, ProductId = item.ProductId, Quantity = item.Quantity, Price = item.Price });
            }
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart"); // Xóa giỏ hàng sau khi đặt thành công
            TempData["SuccessMessage"] = "🎉 Đặt hàng thành công! Mã đơn hàng của bạn là #" + order.Id + ". Chúng tôi sẽ liên hệ sớm nhất.";
            return RedirectToAction("Index", "Home");
        }
    }
}