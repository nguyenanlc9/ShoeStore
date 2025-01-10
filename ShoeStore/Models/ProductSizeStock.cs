using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("ProductSizeStocks")]
    public class ProductSizeStock
    {
        [Key]
        public int ProductSizeStockID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        [Display(Name = "Sản phẩm")]
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kích thước")]
        [Display(Name = "Kích thước")]
        public int SizeID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn")]
        [Display(Name = "Số lượng tồn")]
        [Range(0, 999, ErrorMessage = "Số lượng tồn phải từ 0 đến 999")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Số lượng tồn chỉ được nhập số")]
        public int StockQuantity { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }

        [ForeignKey("SizeID")]
        public virtual Size Size { get; set; }
    }
}