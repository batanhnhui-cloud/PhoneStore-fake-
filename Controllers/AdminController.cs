using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhoneStore.Data;
using PhoneStore.Models;

namespace PhoneStore.Controllers
{
    public class TopProductVM
    {
        public string ProductName { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

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
        // 0. BẢNG ĐIỀU KHIỂN TỔNG QUAN
        // ==========================================
        public async Task<IActionResult> Dashboard()
        {
            // Doanh thu (Chỉ tính đơn Success)
            ViewBag.TotalRevenue = await _context.Orders.Where(o => o.Status == "Success").SumAsync(o => o.TotalAmount);

            // Tổng đơn hàng
            ViewBag.TotalOrders = await _context.Orders.CountAsync();

            // TỔNG MÁY TRONG KHO (Đếm chính xác từng mã IMEI đang rảnh)
            ViewBag.TotalDevices = await _context.DeviceImeis.CountAsync(d => d.Status == "Available");

            // Tổng số chi nhánh
            ViewBag.TotalBranches = await _context.Branches.CountAsync();

            // Lấy 5 đơn hàng gần nhất
            var recentOrders = await _context.Orders
                .Include(o => o.Branch)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            return View(recentOrders);
        }

        // ==========================================
        // 1. QUẢN LÝ ĐƠN HÀNG TOÀN QUỐC (CHO ADMIN)
        // ==========================================
        public async Task<IActionResult> OrderManagement()
        {
            var orders = await _context.Orders
                .Include(o => o.Branch)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{order.Id} thành công!";
            }
            return RedirectToAction(nameof(OrderManagement));
        }

        // NÚT XÓA ĐƠN HÀNG MỚI THÊM
        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn đơn hàng #{orderId} thành công!";
            }
            return RedirectToAction(nameof(OrderManagement));
        }

        // ==========================================
        // 2. QUẢN LÝ TỒN KHO TOÀN QUỐC
        // ==========================================
        public async Task<IActionResult> ManageInventory()
        {
            // Gom nhóm tất cả mã IMEI Sẵn sàng bán theo từng Chi nhánh và Sản phẩm
            // (Sử dụng lại class InventoryStatVM đã tạo bên StaffController cho tiện)
            var stats = await _context.DeviceImeis
                .Include(d => d.Product)
                .Include(d => d.Branch)
                .Where(d => d.Status == "Available")
                .GroupBy(d => new { d.BranchId, BranchName = d.Branch!.Name, d.ProductId, ProductName = d.Product!.Name })
                .Select(g => new PhoneStore.Controllers.InventoryStatVM
                {
                    BranchName = g.Key.BranchName,
                    ProductName = g.Key.ProductName,
                    AvailableCount = g.Count()
                })
                .OrderBy(x => x.BranchName).ThenBy(x => x.ProductName)
                .ToListAsync();

            return View(stats);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int inventoryId, int newQuantity, int? filterBranchId)
        {
            var inv = await _context.Inventories.FindAsync(inventoryId);
            if (inv != null)
            {
                inv.StockQuantity = newQuantity >= 0 ? newQuantity : 0;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật tồn kho thành công!";
            }
            return RedirectToAction(nameof(ManageInventory), new { branchId = filterBranchId });
        }

        // ==========================================
        // 3. QUẢN LÝ KHÁCH HÀNG (CRM)
        // ==========================================
        public async Task<IActionResult> ManageCustomers()
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var staffs = await _userManager.GetUsersInRoleAsync("Staff");
            var excludeIds = admins.Select(u => u.Id).Union(staffs.Select(u => u.Id)).ToList();

            var customers = await _userManager.Users.Where(u => !excludeIds.Contains(u.Id)).ToListAsync();
            var customerList = new List<CustomerViewModel>();

            foreach (var cus in customers)
            {
                var orders = await _context.Orders.Where(o => o.UserId == cus.Id && o.Status == "Success").ToListAsync();
                var totalSpent = orders.Sum(o => o.TotalAmount);

                string rank = "ĐỒNG";
                if (totalSpent >= 50000000) rank = "VÀNG";
                else if (totalSpent >= 20000000) rank = "BẠC";

                customerList.Add(new CustomerViewModel
                {
                    Id = cus.Id,
                    FullName = cus.FullName ?? "Khách vãng lai",
                    Email = cus.Email,
                    PhoneNumber = cus.PhoneNumber ?? "Chưa cập nhật",
                    TotalSpent = totalSpent,
                    TotalOrders = orders.Count,
                    Rank = rank
                });
            }
            return View(customerList.OrderByDescending(c => c.TotalSpent).ToList());
        }

        public async Task<IActionResult> EditCustomer(string id) { var user = await _userManager.FindByIdAsync(id); if (user == null) return NotFound(); return View(user); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(string id, string fullName, string phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(id); if (user == null) return NotFound();
            user.FullName = fullName; user.PhoneNumber = phoneNumber;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return RedirectToAction(nameof(ManageCustomers));
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(user);
        }
        public async Task<IActionResult> DeleteCustomer(string id) { var user = await _userManager.FindByIdAsync(id); if (user != null) await _userManager.DeleteAsync(user); return RedirectToAction(nameof(ManageCustomers)); }

        // ==========================================
        // 4. QUẢN LÝ CHI NHÁNH & NHÂN SỰ & BÁO CÁO
        // ==========================================
        public async Task<IActionResult> ManageBranches() => View(await _context.Branches.ToListAsync());
        public IActionResult CreateBranch() => View();
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBranch([Bind("Name,Address")] Branch branch)
        {
            ModelState.Remove("Users"); ModelState.Remove("Inventories"); ModelState.Remove("Orders");
            if (ModelState.IsValid)
            {
                _context.Add(branch); await _context.SaveChangesAsync();
                var products = await _context.Products.ToListAsync();
                foreach (var p in products) _context.Inventories.Add(new Inventory { ProductId = p.Id, BranchId = branch.Id, StockQuantity = 0 });
                await _context.SaveChangesAsync(); return RedirectToAction(nameof(ManageBranches));
            }
            return View(branch);
        }
        public async Task<IActionResult> EditBranch(int id) { var branch = await _context.Branches.FindAsync(id); if (branch == null) return NotFound(); return View(branch); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBranch(int id, [Bind("Id,Name,Address")] Branch branch)
        {
            if (id != branch.Id) return NotFound();
            ModelState.Remove("Users"); ModelState.Remove("Inventories"); ModelState.Remove("Orders");
            if (ModelState.IsValid) { _context.Update(branch); await _context.SaveChangesAsync(); return RedirectToAction(nameof(ManageBranches)); }
            return View(branch);
        }
        public async Task<IActionResult> DeleteBranch(int id) { var branch = await _context.Branches.FindAsync(id); if (branch != null) { _context.Branches.Remove(branch); await _context.SaveChangesAsync(); } return RedirectToAction(nameof(ManageBranches)); }

        public async Task<IActionResult> ManageStaff()
        {
            var staffInRole = await _userManager.GetUsersInRoleAsync("Staff"); var staffIds = staffInRole.Select(s => s.Id).ToList();
            var staffList = await _context.Users.Include(u => u.Branch).Where(u => staffIds.Contains(u.Id)).ToListAsync(); return View(staffList);
        }
        public IActionResult CreateStaff() { ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name"); return View(); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(string fullName, string email, string password, int branchId)
        {
            var branchExists = await _context.Branches.AnyAsync(b => b.Id == branchId);
            if (!branchExists) { ModelState.AddModelError("", "Vui lòng chọn một chi nhánh hợp lệ."); ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name"); return View(); }
            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, BranchId = branchId };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded) { await _userManager.AddToRoleAsync(user, "Staff"); return RedirectToAction(nameof(ManageStaff)); }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", branchId); return View();
        }
        public async Task<IActionResult> EditStaff(string id) { var user = await _userManager.FindByIdAsync(id); if (user == null) return NotFound(); ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", user.BranchId); return View(user); }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(string id, string fullName, string email, int branchId, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id); if (user == null) return NotFound();
            user.FullName = fullName; user.Email = email; user.UserName = email; user.BranchId = branchId;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) { if (!string.IsNullOrWhiteSpace(newPassword)) { await _userManager.RemovePasswordAsync(user); await _userManager.AddPasswordAsync(user, newPassword); } return RedirectToAction(nameof(ManageStaff)); }
            ViewBag.Branches = new SelectList(_context.Branches, "Id", "Name", branchId); return View(user);
        }
        public async Task<IActionResult> DeleteStaff(string id) { var user = await _userManager.FindByIdAsync(id); if (user != null) await _userManager.DeleteAsync(user); return RedirectToAction(nameof(ManageStaff)); }

        public async Task<IActionResult> RevenueReport()
        {
            var report = await _context.Orders.Where(o => o.Status == "Success").Include(o => o.Branch)
                .GroupBy(o => o.Branch.Name).Select(g => new { BranchName = g.Key ?? "Vãng lai", TotalAmount = g.Sum(o => o.TotalAmount) })
                .ToDictionaryAsync(x => x.BranchName, x => x.TotalAmount);
            if (report == null) report = new Dictionary<string, decimal>(); return View(report);
        }
    }
}