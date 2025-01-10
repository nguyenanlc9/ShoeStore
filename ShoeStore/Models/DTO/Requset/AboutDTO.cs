using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ShoeStore.Models.DTO.Requset
{
    public class AboutDTO
    {
        public int AboutId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Content { get; set; }

        public string? ImagePath { get; set; }

        public IFormFile? ImageFile { get; set; }

        public int Status { get; set; } = 1;
    }
} 