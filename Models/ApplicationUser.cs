using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models
{
    // Kế thừa IdentityUser để thêm các thuộc tính tùy chỉnh
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        // Liên kết với chi nhánh (Dành cho mô hình chuỗi)
        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}