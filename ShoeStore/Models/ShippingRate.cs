using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class ShippingRate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Province { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseFee { get; set; }

        [Required]
        public int DeliveryDays { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
} 