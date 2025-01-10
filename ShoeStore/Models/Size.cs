using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Sizes")]
    public class Size
    {
        [Key]
        public int SizeID { get; set; }

        [Required(ErrorMessage = "Kích thước không được để trống")]
        [Display(Name = "Kích thước")]
        [Column(TypeName = "decimal(3,1)")]
        [Range(35.0, 46.0, ErrorMessage = "Kích thước phải từ 35 đến 46")]
        [RegularExpression(@"^\d+(\.\d)?$", ErrorMessage = "Kích thước chỉ được nhập số và một số thập phân")]
        public decimal SizeValue { get; set; }

        [Required(ErrorMessage = "Tên kích thước không được để trống")]
        [Display(Name = "Tên kích thước")]
        [StringLength(50, ErrorMessage = "Tên kích thước không được vượt quá 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Tên kích thước chỉ được chứa chữ cái, số và khoảng trắng")]
        public string SizeName { get; set; }

        // Navigation property
        public virtual ICollection<ProductSizeStock>? ProductSizeStocks { get; set; }

        public Size()
        {
            // Tự động set SizeName bằng SizeValue khi tạo mới
            SizeName = SizeValue.ToString();
        }
    }
} 