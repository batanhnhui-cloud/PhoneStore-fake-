using Microsoft.AspNetCore.Identity;

namespace PhoneStore.Models
{
    // Lớp này chỉ xuất hiện duy nhất ở đây để tránh lỗi CS0101
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}