using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class ReturnRequest
    {
        [Key]
        public int ReturnId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        public string? Images { get; set; }

        public ReturnStatus Status { get; set; }

        public string? AdminNote { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }

    public enum ReturnStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }
} 