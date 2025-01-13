using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ShoeStore.Models.DTO.Requset
{
    public class AboutDTO
    {
        public int AboutId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(50)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        [StringLength(255)]
        public string Content { get; set; }

        public string? ImagePath { get; set; }

        public IFormFile? ImageFile { get; set; }

        public int Status { get; set; } = 1;
    }
} 