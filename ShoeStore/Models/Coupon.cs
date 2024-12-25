using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    [Table("Coupon")]
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }

        [Required(ErrorMessage = "Mã giảm giá là bắt buộc.")]
        [StringLength(20, ErrorMessage = "Mã giảm giá không được vượt quá 20 ký tự.")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã giảm giá chỉ được chứa chữ cái in hoa và số.")]
        public string? CouponCode { get; set; }

        [StringLength(20, ErrorMessage = "Tên mã giảm giá không được vượt quá 20 ký tự.")]
        public string? CouponName { get; set; }

        [Required(ErrorMessage = "Phần trăm giảm giá là bắt buộc.")]
        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải nằm trong khoảng từ 0 đến 100.")]
        public decimal DiscountPercentage { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc.")]
        public DateTime DateStart { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc.")]
        public DateTime DateEnd { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Nhập số lượng hợp lệ.")]
        public int Quantity { get; set; }

        public bool Status { get; set; }

        public bool IsValid()
        {
            return Status &&
                   DateStart <= DateTime.Now &&
                   DateEnd >= DateTime.Now &&
                   Quantity > 0;
        }
    }
}
