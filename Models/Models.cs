using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models
{
    public class Branch
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        public string? Address { get; set; }
        public virtual ICollection<ApplicationUser>? Staffs { get; set; }
        public virtual ICollection<Inventory>? Inventories { get; set; }
        public virtual ICollection<DeviceImei> DeviceImeis { get; set; } = new List<DeviceImei>();
    }

    public class Inventory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }
        public int BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
        public int StockQuantity { get; set; }
    }
}