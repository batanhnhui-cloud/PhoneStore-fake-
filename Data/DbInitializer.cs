using Microsoft.AspNetCore.Identity;
using PhoneStore.Models; // Đảm bảo namespace này trỏ đúng đến thư mục chứa ApplicationUser

namespace PhoneStore.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // SỬA TẠI ĐÂY: Thay IdentityUser bằng ApplicationUser
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Tạo các Vai trò (Roles)
            string[] roleNames = { "Admin", "Staff", "Customer" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Tạo tài khoản Admin mặc định
            var adminEmail = "admin@phonestore.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // SỬA TẠI ĐÂY: Khởi tạo ApplicationUser thay vì IdentityUser
                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Hệ Thống Admin" // Bạn có thể thêm các trường tùy chỉnh ở đây
                };

                var createPowerUser = await userManager.CreateAsync(user, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}