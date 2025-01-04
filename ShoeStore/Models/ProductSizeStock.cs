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
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
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