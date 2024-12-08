using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; } // Display as Product ID
        public string Name { get; set; }   // Display as Name
        public decimal Price { get; set; } // Display as Price
        public string Description { get; set; } // Display as Description
        public string ImagePath { get; set; }   // Display as Image Path
        public int StockQuantity { get; set; }  // Display as Stock Quantity



        [ForeignKey("CategoryId")]
        public virtual Category? Categories { get; set; }

        [ForeignKey("BrandId")]

        public virtual Brand? Brands { get; set; }



    }
}
