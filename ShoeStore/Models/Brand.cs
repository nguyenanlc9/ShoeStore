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

        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự.")]
        [DisplayName("Brand Name")]
        public required string Name { get; set; }
        [DisplayName("DisplayOrder")]
        public int DisplayOrder { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
    }

}
