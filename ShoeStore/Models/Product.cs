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

        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải là số lớn hơn 0.")]
        public decimal Price { get; set; } 
        public string Description { get; set; }

        [StringLength(5)]
        public decimal DiscountPrice { get; set; }
        public string? ImagePath { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải là số lớn hơn 0.")]

        [StringLength(5)]
        public int StockQuantity { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }


        [ForeignKey("CategoryId")]
        public virtual Category? Categories { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brands { get; set; }

    }
}
