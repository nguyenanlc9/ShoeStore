using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class BlogImage
    {
        [Key]
        public int ImgId { get; set; }

        public int Blog_ID { get; set; }

        [Required]
        public string Img { get; set; }

        public bool IsMainImage { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("Blog_ID")]
        public virtual Blog Blog { get; set; }
    }
}
