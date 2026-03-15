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
            _context = context;
            _userManager = userManager;
        }

        // --- QUẢN LÝ CHI NHÁNH ---
        public async Task<IActionResult> ManageBranches()
        {
            return View(await _context.Branches.ToListAsync());
        }

        public IActionResult CreateBranch() => View();

        [HttpPost]
        public async Task<IActionResult> CreateBranch(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Add(branch);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageBranches));
            }
            return View(branch);
        }

        // --- QUẢN LÝ NHÂN VIÊN ---
        public async Task<IActionResult> ManageStaff()
        {
            var staffList = await _userManager.GetUsersInRoleAsync("Staff");
            return View(staffList);
        }

        public async Task<IActionResult> CreateStaff()
        {
            // Gửi danh sách chi nhánh sang View để Admin chọn cho nhân viên
            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(string email, string password, string fullName, int branchId)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                BranchId = branchId,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Staff");
                return RedirectToAction(nameof(ManageStaff));
            }

            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
            return View();
        }
    }
}