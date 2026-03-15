using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;

namespace PhoneStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            // 1. Lấy tất cả sản phẩm
            var products = _context.Products.Include(p => p.Category).AsQueryable();

            // 2. Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.Name.Contains(searchString));
            }

            // 3. Lọc theo hãng (Category)
            if (categoryId.HasValue)
            {
                products = products.Where(x => x.CategoryId == categoryId);
            }

            // Gửi danh sách hãng sang View để làm menu lọc
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(await products.ToListAsync());
        }
    }
}