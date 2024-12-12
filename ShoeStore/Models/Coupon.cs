using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    [Table("Coupon")]
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }
        public string? CouponName { get; set; }
        public string? Description { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public int Quantity { get; set; }
        public bool Status { get; set; }
    }
}
