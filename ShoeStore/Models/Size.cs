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
        public decimal SizeValue { get; set; }

        [Required(ErrorMessage = "Tên kích thước không được để trống")]
        [Display(Name = "Tên kích thước")]
        [StringLength(50)]
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