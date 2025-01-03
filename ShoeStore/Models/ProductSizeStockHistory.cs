using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("ProductSizeStockHistories")]
    public class ProductSizeStockHistory
    {
        [Key]
        public int HistoryId { get; set; }
        
        public int ProductId { get; set; }
        public int SizeId { get; set; }
        
        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }
        
        public string ModifiedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Action { get; set; } // "Create", "Update", "Delete"

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("SizeId")]
        public virtual Size Size { get; set; }
    }
} 