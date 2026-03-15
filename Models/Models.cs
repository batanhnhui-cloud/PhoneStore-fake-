using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models
{
    // 1. Quản lý chi nhánh
    public class Branch
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        public string Address { get; set; }
        public virtual ICollection<Inventory> Inventories { get; set; }
        public virtual ICollection<ApplicationUser> Staffs { get; set; }
    }

    // 2. Tồn kho riêng cho từng chi nhánh
    public class Inventory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        public int BranchId { get; set; }
        public virtual Branch Branch { get; set; }
        public int StockQuantity { get; set; } // Số lượng máy tại chi nhánh này
    }

    // 3. Mở rộng User để biết nhân viên thuộc chi nhánh nào
    using Microsoft.AspNetCore.Identity;
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public int? BranchId { get; set; } // Nếu là Staff thì phải có BranchId
        public virtual Branch Branch { get; set; }
    }
}