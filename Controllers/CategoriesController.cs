using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Categories.ToListAsync());

        public IActionResult Create() => View();
        [HttpPost]
        public async Task<IActionResult> Create(Category cat)
        {
            if (ModelState.IsValid) { _context.Add(cat); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
            return View(cat);
        }

        // --- SỬA HÃNG ---
        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            return View(cat);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Category cat)
        {
            if (ModelState.IsValid) { _context.Update(cat); await _context.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
            return View(cat);
        }

        // --- XÓA HÃNG ---
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat != null) { _context.Categories.Remove(cat); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}