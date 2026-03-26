using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Models
{
    public class ImeiTransfer
    {
        [Key]
        public int Id { get; set; }

        // Chiếc máy nào đang được chuyển?
        public int DeviceImeiId { get; set; }
        [ForeignKey("DeviceImeiId")]
        public virtual DeviceImei? DeviceImei { get; set; }

        // Chuyển từ đâu?
        public int FromBranchId { get; set; }
        [ForeignKey("FromBranchId")]
        public virtual Branch? FromBranch { get; set; }

        // Chuyển đến đâu?
        public int ToBranchId { get; set; }
        [ForeignKey("ToBranchId")]
        public virtual Branch? ToBranch { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.Now; // Ngày xuất kho đi
        public DateTime? ReceiveDate { get; set; }                 // Ngày chi nhánh kia nhận được

        // Trạng thái: "Pending" (Đang đi trên đường), "Completed" (Đã nhập kho đích)
        public string Status { get; set; } = "Pending";
    }
}