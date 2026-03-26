using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;
using System.Diagnostics;

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

        // 1. TRANG CHỦ
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.Include(p => p.Category).OrderByDescending(p => p.Id).Take(12).ToListAsync();
            return View(products);
        }

        // 2. TRANG DANH SÁCH SẢN PHẨM (SHOP)
        public async Task<IActionResult> Shop(int? categoryId, string? searchString)
        {
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue) products = products.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrEmpty(searchString)) products = products.Where(p => p.Name.Contains(searchString));

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSearch = searchString;

            return View(await products.OrderByDescending(p => p.Id).ToListAsync());
        }

        // 3. TRANG CHI TIẾT SẢN PHẨM (MỚI THÊM)
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

        // 4. LỊCH SỬ ĐƠN HÀNG
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

        // ==========================================
        // TRA CỨU LỊCH SỬ MUA HÀNG (CHỈ CẦN SĐT)
        // ==========================================
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

            // Lấy TẤT CẢ đơn hàng khớp với số điện thoại, sắp xếp đơn mới nhất lên đầu
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.DeviceImeis) // Lấy danh sách IMEI
                .Include(o => o.Branch)
                .Where(o => o.Phone == phone.Trim())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            if (!orders.Any())
            {
                ViewBag.Error = $"Không tìm thấy lịch sử mua hàng nào với số điện thoại {phone}.";
                return View();
            }

            // Trả về một Danh sách đơn hàng
            return View(orders);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}