using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        public int ProductId { get; set; }
        
        public int UserId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 