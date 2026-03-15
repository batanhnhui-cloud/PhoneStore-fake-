using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        // Đã bổ sung UserManager vào đây
        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // --- ĐĂNG NHẬP ---
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
                if (result.Succeeded) return LocalRedirect(returnUrl ?? "/");
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác!");
            }
            else ModelState.AddModelError(string.Empty, "Vui lòng nhập đầy đủ Email và Mật khẩu.");

            return View();
        }

        // --- ĐĂNG KÝ TÀI KHOẢN MỚI ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return View();
            }

            if (ModelState.IsValid)
            {
                // 1. Tạo mới tài khoản
                var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName };
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // 2. Tự động gán quyền "Customer" (Khách hàng) cho người mới
                    if (!await _userManager.IsInRoleAsync(user, "Customer"))
                    {
                        await _userManager.AddToRoleAsync(user, "Customer");
                    }

                    // 3. Cho phép đăng nhập luôn mà không cần xác nhận email
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                // Hiển thị lỗi nếu mật khẩu quá yếu hoặc email đã tồn tại
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View();
        }

        
        // --- ĐĂNG XUẤT ---
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();
    }
}