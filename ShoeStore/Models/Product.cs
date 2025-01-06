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
        [Range(1, 999999999999, ErrorMessage = "Giá phải từ 1đ đến 999,999,999,999đ")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Giá chỉ được nhập số")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Mô tả sản phẩm là bắt buộc.")]
        public string? Description { get; set; }

        [Range(0, 999999999999, ErrorMessage = "Giá khuyến mãi phải từ 0đ đến 999,999,999,999đ")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Giá khuyến mãi chỉ được nhập số")]
        public decimal DiscountPrice { get; set; } = 0;
        
        public string? ImagePath { get; set; }

        [Required(ErrorMessage = "Trạng thái sản phẩm là bắt buộc")]
        [Display(Name = "Trạng thái")]
        public ProductStatus Status { get; set; } = ProductStatus.Available;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
        
        public string? UpdatedBy { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Categories { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brands { get; set; }

        public virtual ICollection<ProductSizeStock>? ProductSizeStocks { get; set; }

        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }

        [Range(0, 5)]
        public int Rating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;

        public int SoldQuantity { get; set; } = 0;

        public string? Images { get; set; }

        public bool IsNew { get; set; }

        public bool IsHot { get; set; }

        public bool IsSale { get; set; }

        public virtual ICollection<ProductImage> ProductImages { get; set; }

        public virtual ICollection<Review> Reviews { get; set; }

        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Mã sản phẩm phải từ 3-20 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "Mã sản phẩm chỉ được chứa chữ, số và dấu gạch ngang")]
        public string ProductCode { get; set; }

        public Product()
        {
            ProductSizeStocks = new HashSet<ProductSizeStock>();
            OrderDetails = new HashSet<OrderDetail>();
            ProductImages = new HashSet<ProductImage>();
            Reviews = new HashSet<Review>();
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
