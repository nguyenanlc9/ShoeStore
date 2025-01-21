using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Abouts")]
    public class About
    {
        [Key]
        public int AboutId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [Display(Name = "Nội dung")]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; }

        public string? ImagePath { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Trạng thái")]
        public int Status { get; set; } = 1; // 1: Hiển thị, 0: Ẩn
    }
} 