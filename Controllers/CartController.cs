using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using System.Text.Json;

namespace PhoneStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Đã bổ sung thêm UserManager vào Constructor
        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString("Cart");
            if (sessionData == null) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(sessionData) ?? new List<CartItem>();
        }

        private void SaveCartSession(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        public IActionResult Index() => View(GetCartItems());

        public IActionResult AddToCart(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == id);

            if (item != null) item.Quantity++;
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            }

            SaveCartSession(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var cart = GetCartItems();
            cart.RemoveAll(c => c.ProductId == id);
            SaveCartSession(cart);
            return RedirectToAction("Index");
        }

        // --- CHỨC NĂNG MỚI: THANH TOÁN ---

        // 1. Hiển thị form nhập thông tin (Bắt buộc phải đăng nhập)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (cart.Count == 0) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);

            // Lấy danh sách chi nhánh để khách hàng chọn nơi xử lý đơn
            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");

            var order = new Order
            {
                CustomerName = user?.FullName ?? user?.UserName, // Tự động điền tên nếu có
            };

            return View(order);
        }

        // 2. Xử lý lưu vào Database khi khách bấm "Đặt hàng"
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = GetCartItems();
            if (cart.Count == 0) return RedirectToAction("Index");

            var user = await _userManager.GetUserAsync(User);

            // Điền các thông tin hệ thống tự tính toán
            order.UserId = user?.Id;
            order.OrderDate = DateTime.Now;
            order.Status = "Pending"; // Trạng thái: Chờ xử lý
            order.TotalAmount = cart.Sum(c => c.Total);

            // Chuyển hàng từ Giỏ (Session) sang Chi tiết hóa đơn (Database)
            foreach (var item in cart)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });

                // Trừ số lượng tồn kho tại chi nhánh khách đã chọn
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.BranchId == order.BranchId);

                if (inventory != null)
                {
                    inventory.StockQuantity -= item.Quantity;
                }
            }

            // Lưu vào Database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Xóa giỏ hàng sau khi đặt thành công
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("CheckoutSuccess");
        }

        // 3. Trang thông báo đặt hàng thành công
        public IActionResult CheckoutSuccess() => View();
    }
}