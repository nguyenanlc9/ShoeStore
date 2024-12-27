using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }

        [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã giảm giá không được vượt quá 20 ký tự")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã giảm giá chỉ được chứa chữ cái in hoa và số")]
        [Display(Name = "Mã giảm giá")]
        public string CouponCode { get; set; }

        [Required(ErrorMessage = "Tên mã giảm giá là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên mã giảm giá không được vượt quá 50 ký tự")]
        [Display(Name = "Tên mã giảm giá")]
        public string CouponName { get; set; }

        [Required(ErrorMessage = "Phần trăm giảm giá là bắt buộc")]
        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải nằm trong khoảng từ 0 đến 100")]
        [Display(Name = "Phần trăm giảm (%)")]
        public decimal DiscountPercentage { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime DateStart { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime DateEnd { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Display(Name = "Trạng thái")]
        public bool Status { get; set; }

        // Các thuộc tính bổ sung cho việc kiểm tra
        [NotMapped]
        public bool IsActive => Status && DateStart <= DateTime.Now && DateEnd >= DateTime.Now && Quantity > 0;

        // Các thuộc tính tương thích với code cũ
        [NotMapped]
        public decimal DiscountPercent => DiscountPercentage;

        [NotMapped]
        public DateTime StartDate => DateStart;

        [NotMapped]
        public DateTime EndDate => DateEnd;

        [NotMapped]
        public string Code => CouponCode;
    }
}
