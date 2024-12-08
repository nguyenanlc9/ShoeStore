using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Brands")]
    public class Brand
    {
        [Key]
        public int BrandId { get; set; } // Display as ID
        public string Name { get; set; } // Display as Name
        public virtual ICollection<Product>? Products { get; set; }
    }

}
