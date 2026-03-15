using System;
using System.ComponentModel.DataAnnotations;
namespace PhoneStore.Models
{
    public class Order
    {
        public int Id { get; set; }
        [Required] public string UserId { get; set; } // Lưu ID người đặt
        [Required] public string CustomerName { get; set; }
        [Required] public string PhoneNumber { get; set; }
        [Required] public string Address { get; set; }
        public decimal TotalAmount { get; set; } // Đồng bộ kiểu decimal
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}