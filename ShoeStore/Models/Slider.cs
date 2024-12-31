using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Sliders")]
    public class Slider
    {
        [Key]
        public int Slider_ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(255)]
        public string Title { get; set; }

        [StringLength(255)] 
        public string Description { get; set; }

        [Required]
        [StringLength(255)]
        public string Img { get; set; }

        [StringLength(255)]
        public string Link { get; set; }

        public int Status { get; set; }

        public int Sort { get; set; }
    }
}
