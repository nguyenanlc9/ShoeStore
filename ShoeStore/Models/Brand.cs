using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Brands")]
    public class Brand
    {
        [Key]
        public int BrandId { get; set; } // Display as ID

        [DisplayName("Brand Name")]
        public required string Name { get; set; }
        [DisplayName("DisplayOrder")]
        public int DisplayOrder { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
    }

}
