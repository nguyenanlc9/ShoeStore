using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace ShoeStore.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Giá không được để trống")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Mô tả không được để trống")]
        public string Description { get; set; }

        public string ImagePath { get; set; }

        public int StockQuantity { get; set; }

        // Thêm Brand relationship
        [Required]
        public int BrandId { get; set; }
        public Brand Brand { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}