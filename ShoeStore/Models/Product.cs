using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a brand")]
        [DisplayName("Brand")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc.")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0.")]
        public decimal Price { get; set; }

        public string Description { get; set; }

        public decimal DiscountPrice { get; set; }
        
        public string? ImagePath { get; set; }

        [Required(ErrorMessage = "Trạng thái sản phẩm là bắt buộc")]
        [Display(Name = "Trạng thái")]
        public ProductStatus Status { get; set; } = ProductStatus.Available; // Mặc định là đang bán

        public DateTime? UpdatedDate { get; set; }
        
        public string? UpdatedBy { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Categories { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brands { get; set; }

        public virtual ICollection<ProductSizeStock>? ProductSizeStocks { get; set; }

        [Range(0, 5)]
        public int Rating { get; set; } = 0;  // Số sao đánh giá trung bình

        public int ReviewCount { get; set; } = 0;  // Số lượng đánh giá

        public int SoldQuantity { get; set; } = 0;

        public Product()
        {
            ProductSizeStocks = new HashSet<ProductSizeStock>();
        }
    }

    public enum ProductStatus
    {
        [Display(Name = "Đang bán")]
        Available = 1,

        [Display(Name = "Hết hàng")]
        OutOfStock = 2,

        [Display(Name = "Ngừng kinh doanh")]
        Discontinued = 3
    }
}
