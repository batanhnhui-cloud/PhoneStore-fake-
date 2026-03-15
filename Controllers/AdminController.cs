using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context; _userManager = userManager;
        }

        // ==========================================
        // 1. QUẢN LÝ CHI NHÁNH (BRANCH)
        // ==========================================

        public async Task<IActionResult> ManageBranches() => View(await _context.Branches.ToListAsync());

        public IActionResult CreateBranch() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        // BIND: Chỉ nhận Tên và Địa chỉ từ Form
        public async Task<IActionResult> CreateBranch([Bind("Name,Address")] Branch branch)
        {
            // Xóa xác thực ngầm định của các danh sách liên kết
            ModelState.Remove("Users");
            ModelState.Remove("Inventories");
            ModelState.Remove("Orders");

            if (ModelState.IsValid)
            {
                _context.Add(branch);
                await _context.SaveChangesAsync();

                var products = await _context.Products.ToListAsync();
                foreach (var p in products)
                {
                    _context.Inventories.Add(new Inventory { ProductId = p.Id, BranchId = branch.Id, StockQuantity = 0 });
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageBranches));
            }
            return View(branch);
        }

        public async Task<IActionResult> EditBranch(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // BIND: Chỉ nhận Id, Tên và Địa chỉ
        public async Task<IActionResult> EditBranch(int id, [Bind("Id,Name,Address")] Branch branch)
        {
            if (id != branch.Id) return NotFound();

            // Ép hệ thống bỏ qua kiểm tra các danh sách
            ModelState.Remove("Users");
            ModelState.Remove("Inventories");
            ModelState.Remove("Orders");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(branch);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BranchExists(branch.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(ManageBranches));
            }
            return View(branch);
        }

        private bool BranchExists(int id) => _context.Branches.Any(e => e.Id == id);

        public async Task<IActionResult> DeleteBranch(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null) { _context.Branches.Remove(branch); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(ManageBranches));
        }

        // ==========================================
        // 2. QUẢN LÝ NHÂN SỰ & BÁO CÁO (GIỮ NGUYÊN)
        // ==========================================
        public async Task<IActionResult> ManageStaff()
        {
            var staffInRole = await _userManager.GetUsersInRoleAsync("Staff");
            var staffIds = staffInRole.Select(s => s.Id).ToList();
            var staffList = await _context.Users.Include(u => u.Branch).Where(u => staffIds.Contains(u.Id)).ToListAsync();
            return View(staffList);
        }

        public IActionResult CreateStaff() { ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name"); return View(); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(string fullName, string email, string password, int branchId)
        {
            // 1. KIỂM TRA BẢO MẬT: Đảm bảo chi nhánh được chọn thực sự tồn tại
            var branchExists = await _context.Branches.AnyAsync(b => b.Id == branchId);
            if (!branchExists)
            {
                ModelState.AddModelError("", "Vui lòng chọn một chi nhánh hợp lệ.");
                ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name");
                return View();
            }

            // 2. TẠO TÀI KHOẢN
            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, BranchId = branchId };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Staff");
                return RedirectToAction(nameof(ManageStaff));
            }

            // 3. NẾU LỖI (VD: Trùng email, mật khẩu yếu...) THÌ BÁO LỖI
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", branchId);
            return View();
        }

        public async Task<IActionResult> EditStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", user.BranchId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(string id, string fullName, string email, int branchId, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            user.FullName = fullName; user.Email = email; user.UserName = email; user.BranchId = branchId;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    await _userManager.RemovePasswordAsync(user);
                    await _userManager.AddPasswordAsync(user, newPassword);
                }
                return RedirectToAction(nameof(ManageStaff));
            }
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", branchId);
            return View(user);
        }

        public async Task<IActionResult> DeleteStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null) await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(ManageStaff));
        }

        public async Task<IActionResult> RevenueReport()
        {
            var report = await _context.Orders.Where(o => o.Status == "Success").Include(o => o.Branch)
                .GroupBy(o => o.Branch.Name).Select(g => new { BranchName = g.Key ?? "Vãng lai", TotalAmount = g.Sum(o => o.TotalAmount) })
                .ToDictionaryAsync(x => x.BranchName, x => x.TotalAmount);
            if (report == null) report = new Dictionary<string, decimal>();
            return View(report);
        }
    }
}