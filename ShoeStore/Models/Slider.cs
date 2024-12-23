using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Sliders")]
    public class Slider
    {
        [Key]
        public int Slider_ID { get; set; }
        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự.")]
        public string? Name { get; set; }
        public string? Link { get; set; }
        public string? Img { get; set; }
    }
}
