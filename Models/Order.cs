using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần thư viện này

namespace PhoneStore.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // SỬA TẠI ĐÂY: Xác định độ chính xác là 18 chữ số, 2 số sau dấu phẩy
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        public int? BranchId { get; set; }
        public virtual Branch? Branch { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}