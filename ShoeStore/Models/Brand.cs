using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Brands")]
    public class Brand
    {
        [Key]
        public int BrandId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        // Navigation property
        public ICollection<Product> Products { get; set; }
    }
}
