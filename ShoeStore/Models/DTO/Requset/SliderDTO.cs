using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ShoeStore.Models.DTO.Requset
{
    public class SliderDTO
    {
        public int Slider_ID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên slider")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string Title { get; set; }

        public string Description { get; set; }

        public string Link { get; set; }

        public int Status { get; set; }

        public int Sort { get; set; }

        public string? ImgPath { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
