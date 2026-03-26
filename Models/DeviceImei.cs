using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Models
{
    public class DeviceImei
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Số IMEI không được để trống")]
        [StringLength(15, MinimumLength = 15, ErrorMessage = "IMEI chuẩn phải có đúng 15 chữ số")]
        public string Imei { get; set; } = null!;

        // 1. Máy này là dòng máy nào? (Ví dụ: iPhone 15 Pro Max)
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // 2. Máy này đang nằm ở kho của Chi nhánh nào?
        public int BranchId { get; set; }
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }

        // 3. Trạng thái máy: "Available" (Sẵn sàng bán), "Sold" (Đã bán), "Transferring" (Đang luân chuyển), "Defective" (Bảo hành/Lỗi)
        public string Status { get; set; } = "Available";

        // 4. Khi xuất bán, ghim máy này vào Đơn hàng nào?
        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // 5. Quản lý Bảo hành
        public DateTime? WarrantyActivationDate { get; set; } // Ngày kích hoạt (Lúc chốt đơn)
        public DateTime? WarrantyExpirationDate { get; set; } // Ngày hết hạn (Thường là +12 tháng)
    }
}