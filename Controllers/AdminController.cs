using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PhoneStore.Controllers
{
    [Authorize(Roles = "Admin")] // Chỉ Admin mới vào được
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Trang danh sách nhân viên
        public async Task<IActionResult> ManageStaff()
        {
            var staffMembers = await _userManager.GetUsersInRoleAsync("Staff");
            return View(staffMembers);
        }

        // Trang tạo nhân viên (GET)
        public IActionResult CreateStaff() => View();

        // Xử lý tạo nhân viên (POST)
        [HttpPost]
        public async Task<IActionResult> CreateStaff(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return View();

            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Staff");
                return RedirectToAction(nameof(ManageStaff));
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View();
        }
    }
}