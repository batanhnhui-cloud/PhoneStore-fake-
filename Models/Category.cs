using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Tên hãng không được để trống")]
        public string Name { get; set; }
        // Liên kết với bảng Sản phẩm
        public ICollection<Product>? Products { get; set; }
    }
}