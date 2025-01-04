using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("ProductHistories")]
    public class ProductHistory
    {
        [Key]
        public int HistoryId { get; set; }
        
        public int ProductId { get; set; }
        
        public string Name { get; set; }
        
        public decimal Price { get; set; }
        
        public decimal DiscountPrice { get; set; }
        
        public string Description { get; set; }
        
        public int CategoryId { get; set; }
        
        public int BrandId { get; set; }
        
        public string ProductCode { get; set; }
        
        public bool IsHot { get; set; }
        
        public bool IsNew { get; set; }
        
        public bool IsSale { get; set; }
        
        public ProductStatus Status { get; set; }
        
        public string ImagePath { get; set; }
        
        public string ModifiedBy { get; set; }
        
        public DateTime ModifiedDate { get; set; }
        
        public string Action { get; set; } // "Create", "Update", "Delete"

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
} 