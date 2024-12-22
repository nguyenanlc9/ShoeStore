using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.DTO.Requset
{
    [Table("Sliders")]
    public class SliderDTO
    {
        [Key]
        public int Slider_ID { get; set; }
        public string? Name { get; set; }
        public string? Link { get; set; }
        public IFormFile? Img { get; set; }

    }
}
