using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("ProductSizeStocks")]
    public class ProductSizeStock
    {
        [Key]
        public int ProductSizeStockID { get; set; }

        [Required(ErrorMessage = "Sản phẩm không được để trống")]
        [Display(Name = "Sản phẩm")]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Kích thước không được để trống")]
        [Display(Name = "Kích thước")]
        public int SizeID { get; set; }

        [Required(ErrorMessage = "Số lượng tồn không được để trống")]
        [Display(Name = "Số lượng tồn")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; }

        // Navigation properties
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }

        [ForeignKey("SizeID")]
        public virtual Size Size { get; set; }
    }
}