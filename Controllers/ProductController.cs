using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    // Giữ Authorize ở đây để bảo vệ các hàm Quản lý (Thêm, Xóa, Sửa)
    [Authorize(Roles = "Admin,Staff")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. TRANG DANH SÁCH SẢN PHẨM TRONG ADMIN
        public async Task<IActionResult> Index() => View(await _context.Products.Include(p => p.Category).ToListAsync());

        // ==========================================
        // 2. HÀM CHI TIẾT SẢN PHẨM (DÀNH CHO KHÁCH)
        // ==========================================
        [AllowAnonymous] // CỰC KỲ QUAN TRỌNG: Cho phép khách hàng vào xem mà không cần Login Admin
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // Lấy thêm 4 máy cùng hãng để gợi ý (nếu cần)
            ViewBag.RelatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4).ToListAsync();

            return View(product);
        }

        // 3. TẠO MỚI SẢN PHẨM
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string path = Path.Combine(_hostEnvironment.WebRootPath, "images/products");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    using (var s = new FileStream(Path.Combine(path, fileName), FileMode.Create)) { await ImageFile.CopyToAsync(s); }
                    product.ImageUrl = "/images/products/" + fileName;
                }
                _context.Add(product);
                await _context.SaveChangesAsync();

                var branches = await _context.Branches.ToListAsync();
                foreach (var b in branches) _context.Inventories.Add(new Inventory { ProductId = product.Id, BranchId = b.Id, StockQuantity = 0 });
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 4. CHỈNH SỬA
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p == null) return NotFound();
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", p.CategoryId);
            return View(p);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product p, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string path = Path.Combine(_hostEnvironment.WebRootPath, "images/products", fileName);
                    using (var s = new FileStream(path, FileMode.Create)) { await ImageFile.CopyToAsync(s); }
                    p.ImageUrl = "/images/products/" + fileName;
                }
                _context.Update(p);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", p.CategoryId);
            return View(p);
        }

        // 5. XÓA
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p != null) { _context.Products.Remove(p); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}