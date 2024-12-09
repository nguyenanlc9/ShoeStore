using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryId { get; set; } // Display as ID

        [DisplayName("CategoryName")]
        public required string Name { get; set; }
        [DisplayName("DisplayOrder")]
        public int DisplayOrder { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
    }

}
