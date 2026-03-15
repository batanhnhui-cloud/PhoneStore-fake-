using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using PhoneStore.Models; // Đảm bảo namespace này trỏ đúng đến thư mục Models của bạn

namespace PhoneStore.Data
{
    // CỰC KỲ QUAN TRỌNG: Phải kế thừa IdentityDbContext để dùng được SaveChangesAsync và Identity
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Khai báo các bảng dữ liệu của bạn
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // Nếu bạn có thêm các bảng khác, hãy khai báo tiếp ở đây
    }
}