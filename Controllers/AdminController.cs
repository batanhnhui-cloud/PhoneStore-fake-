using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    // Chỉ những tài khoản có quyền "Admin" mới được phép truy cập vào Controller này
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

        // ==========================================
        // 1. QUẢN LÝ CHI NHÁNH (BRANCH MANAGEMENT)
        // ==========================================

        // Hiển thị danh sách chi nhánh
        public async Task<IActionResult> ManageBranches()
        {
            return View(await _context.Branches.ToListAsync());
        }

        // Mở trang tạo chi nhánh mới
        public IActionResult CreateBranch()
        {
            return View();
        }

        // Xử lý lưu chi nhánh mới và tự động tạo kho hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBranch(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Add(branch);
                await _context.SaveChangesAsync();

                // TỰ ĐỘNG HÓA: Khi có chi nhánh mới, tạo ngay bản ghi tồn kho 
                // cho tất cả các sản phẩm hiện có trong hệ thống với số lượng = 0
                var products = await _context.Products.ToListAsync();
                foreach (var product in products)
                {
                    _context.Inventories.Add(new Inventory
                    {
                        ProductId = product.Id,
                        BranchId = branch.Id,
                        StockQuantity = 0
                    });
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(ManageBranches));
            }
            return View(branch);
        }

        // Mở trang sửa chi nhánh
        public async Task<IActionResult> EditBranch(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        // Xử lý cập nhật thông tin chi nhánh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBranch(Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Update(branch);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageBranches));
            }
            return View(branch);
        }

        // Xử lý xóa chi nhánh
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageBranches));
        }


        // ==========================================
        // 2. QUẢN LÝ NHÂN SỰ (STAFF MANAGEMENT)
        // ==========================================

        // Hiển thị danh sách nhân viên và chi nhánh họ đang làm việc
        public async Task<IActionResult> ManageStaff()
        {
            // Lấy danh sách ID của những người có Role là Staff
            var staffInRole = await _userManager.GetUsersInRoleAsync("Staff");
            var staffIds = staffInRole.Select(s => s.Id).ToList();

            // Truy vấn thông tin chi tiết kèm theo dữ liệu Chi nhánh
            var staffList = await _context.Users
                .Include(u => u.Branch)
                .Where(u => staffIds.Contains(u.Id))
                .ToListAsync();

            return View(staffList);
        }

        // Mở trang tạo tài khoản nhân viên
        public IActionResult CreateStaff()
        {
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name");
            return View();
        }

        // Xử lý tạo tài khoản nhân viên và gán quyền Staff
        [HttpPost]
        public async Task<IActionResult> CreateStaff(string fullName, string email, string password, int branchId)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    BranchId = branchId
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Gán quyền Staff cho tài khoản mới tạo
                    await _userManager.AddToRoleAsync(user, "Staff");
                    return RedirectToAction(nameof(ManageStaff));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name");
            return View();
        }

        // Xử lý xóa tài khoản nhân viên
        public async Task<IActionResult> DeleteStaff(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(ManageStaff));
        }


        // ==========================================
        // 3. BÁO CÁO DOANH THU (REVENUE REPORT)
        // ==========================================

        // Thống kê doanh thu theo từng chi nhánh
        public async Task<IActionResult> RevenueReport()
        {
            var report = await _context.Orders
                .Where(o => o.Status == "Success") // Chỉ tính các đơn hàng đã hoàn thành
                .Include(o => o.Branch)
                .GroupBy(o => o.Branch.Name)
                .Select(g => new
                {
                    BranchName = g.Key ?? "Không xác định",
                    TotalAmount = g.Sum(o => o.TotalAmount)
                })
                .ToDictionaryAsync(x => x.BranchName, x => x.TotalAmount);

            return View(report);
        }
    }
}