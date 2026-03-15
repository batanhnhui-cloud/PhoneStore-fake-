using Microsoft.AspNetCore.Identity;
namespace PhoneStore.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}