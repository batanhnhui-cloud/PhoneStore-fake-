using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PhoneStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. TRANG CHỦ (SUNMOBILE INDEX)
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(12)
                .ToListAsync();
            return View(products);
        }

        // 2. TRANG DANH SÁCH SẢN PHẨM (SHOP) - ĐÃ FIX LỖI DROPDOWN
        public async Task<IActionResult> Shop(int? categoryId, string? searchString)
        {
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // Bộ lọc tìm kiếm theo tên
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(searchString));
            }

            // Bộ lọc theo hãng (Category)
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // QUAN TRỌNG: Fix lỗi "Cannot implicitly convert type... to SelectListItem"
            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = categoryId.HasValue && c.Id == categoryId.Value
            }).ToList();

            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSearch = searchString;

            var result = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();
            return View(result);
        }

        // 3. TRANG CHI TIẾT SẢN PHẨM
        // Lưu ý: Nếu Controller quản lý máy của bạn là ProductsController thì hàm này có thể không chạy.
        // Nhưng nếu bạn dùng link asp-controller="Home" asp-action="Details" thì nó sẽ chạy hàm này.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // Lấy 4 sản phẩm cùng hãng để làm gợi ý mua sắm
            ViewBag.RelatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4).ToListAsync();

            return View(product);
        }

        // 4. LỊCH SỬ ĐƠN HÀNG (CHO USER ĐÃ ĐĂNG NHẬP)
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // 5. TRA CỨU LỊCH SỬ MUA HÀNG (CHỈ CẦN SĐT - KHÔNG CẦN LOGIN)
        [HttpGet]
        public IActionResult TrackOrder()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TrackOrder(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                ViewBag.Error = "Vui lòng nhập số điện thoại để tra cứu!";
                return View();
            }

            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.DeviceImeis)
                .Include(o => o.Branch)
                .Where(o => o.Phone == phone.Trim())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (!orders.Any())
            {
                ViewBag.Error = $"Không tìm thấy lịch sử mua hàng nào với số điện thoại {phone}.";
                return View();
            }

            return View(orders);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}