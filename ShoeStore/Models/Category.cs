using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryId { get; set; } // Display as ID
        public string Name { get; set; } // Display as Name

        public virtual ICollection<Product>? Products { get; set; }
    }

}
