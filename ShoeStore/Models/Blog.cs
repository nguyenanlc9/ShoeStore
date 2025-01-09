using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class Blog
    {
        [Key]
        public int BlogId { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [Display(Name = "Tiêu đề")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [Display(Name = "Nội dung")]
        [Column(TypeName = "ntext")]
        public string Content { get; set; }

        [Display(Name = "Hình ảnh đại diện")]
        public string? ThumbnailImage { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Tác giả")]
        [StringLength(100)]
        public string? Author { get; set; }

        [Display(Name = "Hiển thị")]
        public bool IsVisible { get; set; } = true;

        // Navigation property
        public virtual ICollection<BlogImage>? BlogImages { get; set; }
    }
} 