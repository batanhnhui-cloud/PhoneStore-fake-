using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần thư viện này

namespace PhoneStore.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int Quantity { get; set; }

        // SỬA TẠI ĐÂY: Tránh việc bị làm tròn giá tiền
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
    }
}