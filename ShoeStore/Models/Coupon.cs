using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    [Table("Coupon")]
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }
        [StringLength(20, ErrorMessage = "Tên sản phẩm không được vượt quá 20 ký tự.")]
        public string? CouponName { get; set; }
        public string? Description { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Nhập Số Lượng Hợp lệ")]
        [StringLength(5)]
        public int Quantity { get; set; }
        public bool Status { get; set; }
    }
}
