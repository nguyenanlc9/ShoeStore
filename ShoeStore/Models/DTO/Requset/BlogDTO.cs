using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.DTO.Requset
{
    public class BlogDTO
    {
        [Key]
        public int Blog_ID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ảnh")]
        public IFormFile? Img { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả")]
        public string Description { get; set; }

        [Range(0, 1)]
        public int Status { get; set; }

        public int Sort { get; set; }

    }
}
