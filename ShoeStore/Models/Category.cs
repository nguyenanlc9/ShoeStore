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

        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự.")]
        [DisplayName("CategoryName")]
        public required string Name { get; set; }
        [DisplayName("DisplayOrder")]
        public int DisplayOrder { get; set; }
        public virtual ICollection<Product>? Products { get; set; }
    }

}
