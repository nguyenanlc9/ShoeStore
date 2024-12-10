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

        public string? Name { get; set; }   
        public decimal Price { get; set; } 
        public string Description { get; set; }

        public decimal DiscountPrice { get; set; }
        public string? ImagePath { get; set; }  
        public int StockQuantity { get; set; }

        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }


        [ForeignKey("CategoryId")]
        public virtual Category? Categories { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brands { get; set; }



    }
}
