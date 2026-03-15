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

        // --- QUẢN LÝ NHÂN VIÊN ---

        // 1. Danh sách nhân viên
        public async Task<IActionResult> ManageStaff()
        {
            var staffInRole = await _userManager.GetUsersInRoleAsync("Staff");
            var staffIds = staffInRole.Select(s => s.Id).ToList();
            var staffList = await _context.Users.Include(u => u.Branch)
                            .Where(u => staffIds.Contains(u.Id)).ToListAsync();
            return View(staffList);
        }

        // 2. Thêm nhân viên mới (GET)
        public IActionResult CreateStaff()
        {
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name");
            return View();
        }

        // 2b. Thêm nhân viên mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(string fullName, string email, string password, int branchId)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, BranchId = branchId };
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Staff");
                    return RedirectToAction(nameof(ManageStaff));
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name");
            return View();
        }

        // 3. Sửa nhân viên (GET)
        public async Task<IActionResult> EditStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", user.BranchId);
            return View(user);
        }

        // 3b. Sửa nhân viên (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(string id, string fullName, string email, int branchId, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 1. Cập nhật thông tin cơ bản
            user.FullName = fullName;
            user.Email = email;
            user.UserName = email;
            user.BranchId = branchId;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // 2. Xử lý đổi mật khẩu (chỉ thực hiện nếu Admin nhập mật khẩu mới)
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    // Xóa mật khẩu cũ
                    await _userManager.RemovePasswordAsync(user);
                    // Thêm mật khẩu mới
                    var passwordResult = await _userManager.AddPasswordAsync(user, newPassword);

                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                            ModelState.AddModelError("", "Lỗi mật khẩu: " + error.Description);

                        ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", branchId);
                        return View(user);
                    }
                }

                return RedirectToAction(nameof(ManageStaff));
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", branchId);
            return View(user);
        }

        // 4. Xóa nhân viên
        public async Task<IActionResult> DeleteStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null) await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(ManageStaff));
        }

        // --- CÁC HÀM QUẢN LÝ CHI NHÁNH & BÁO CÁO (Giữ nguyên như bản trước) ---
        public async Task<IActionResult> ManageBranches() => View(await _context.Branches.ToListAsync());
        public IActionResult CreateBranch() => View();
        [HttpPost] public async Task<IActionResult> CreateBranch(Branch b) { /* Code cũ */ return RedirectToAction(nameof(ManageBranches)); }
        public async Task<IActionResult> EditBranch(int id) { /* Code cũ */ return View(); }
        public async Task<IActionResult> RevenueReport()
        {
            // Lấy danh sách doanh thu theo từng chi nhánh
            // Chỉ tính các đơn hàng có trạng thái "Success"
            var report = await _context.Orders
                .Where(o => o.Status == "Success")
                .Include(o => o.Branch)
                .GroupBy(o => o.Branch.Name)
                .Select(g => new
                {
                    BranchName = g.Key ?? "Vãng lai/Không xác định",
                    TotalAmount = g.Sum(o => o.TotalAmount)
                })
                .ToDictionaryAsync(x => x.BranchName, x => x.TotalAmount);

            // Nếu không có dữ liệu, khởi tạo một Dictionary trống thay vì để null
            if (report == null) report = new Dictionary<string, decimal>();

            return View(report); // PHẢI CÓ 'report' TRONG NGOẶC
        }
    
    }
}