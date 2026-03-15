using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context; _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index() => View(await _context.Products.Include(p => p.Category).ToListAsync());

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
                    string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "images/products");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    using (var s = new FileStream(Path.Combine(uploadDir, fileName), FileMode.Create)) { await ImageFile.CopyToAsync(s); }
                    product.ImageUrl = "/images/products/" + fileName;
                }
                _context.Add(product);
                await _context.SaveChangesAsync();

                // Tự động tạo kho cho máy mới ở tất cả chi nhánh
                var branches = await _context.Branches.ToListAsync();
                foreach (var b in branches) _context.Inventories.Add(new Inventory { ProductId = product.Id, BranchId = b.Id, StockQuantity = 0 });
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var p = await _context.Products.FindAsync(id);
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
                _context.Update(p); await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", p.CategoryId);
            return View(p);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.Products.FindAsync(id);
            if (p != null) { _context.Products.Remove(p); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}