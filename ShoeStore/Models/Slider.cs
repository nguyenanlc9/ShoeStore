using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Sliders")]
    public class Slider
    {
        [Key]
        public int Slider_ID { get; set; }

        [Required(ErrorMessage = "Tên slider không được để trống")]
        [StringLength(100, ErrorMessage = "Tên slider không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\u0100-\u017F\u0180-\u024F\u1E00-\u1EFF]+$", ErrorMessage = "Tên slider chỉ được chứa chữ cái, số và khoảng trắng")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\u0100-\u017F\u0180-\u024F\u1E00-\u1EFF]+$", ErrorMessage = "Tiêu đề chỉ được chứa chữ cái, số và khoảng trắng")]
        public string Title { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")] 
        public string Description { get; set; }

        [Required(ErrorMessage = "Hình ảnh không được để trống")]
        [StringLength(255)]
        public string Img { get; set; }

        [Required(ErrorMessage = "Liên kết không được để trống")]
        [StringLength(255)]
        [RegularExpression(@"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$", ErrorMessage = "Liên kết phải là một URL hợp lệ")]
        public string Link { get; set; }

        public int Status { get; set; }

        [Required(ErrorMessage = "Thứ tự không được để trống")]
        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự không được âm")]
        public int Sort { get; set; }
    }
}
