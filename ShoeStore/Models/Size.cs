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
        [Column(TypeName = "decimal(3,1)")]  // Cho phép lưu size dạng số thập phân như 40.5
        public decimal SizeValue { get; set; }


        // Navigation property cho ProductSizeStock
        public virtual ICollection<ProductSizeStock>? ProductSizeStocks { get; set; }
    }
} 