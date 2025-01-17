using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ShoeStore.Models.Enums;

namespace ShoeStore.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [StringLength(50)]
        [Display(Name = "Tên người đặt")]
        public string OrderUsName { get; set; }

        [Display(Name = "Mã đơn hàng")]
        public string OrderCode { get; set; }

        [Display(Name = "Mô tả")]
        public string? OrderDescription { get; set; }

        [Display(Name = "Mã giảm giá")]
        public string? OrderCoupon { get; set; }

        public DateTime OrderDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public OrderStatus Status { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        public int? CouponId { get; set; }

        [ForeignKey("CouponId")]
        public Coupon Coupon { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; }

        [Display(Name = "Trạng thái thanh toán")]
        public PaymentStatus PaymentStatus { get; set; }

        [Display(Name = "Trạng thái đơn hàng")]
        public bool OrderStatus { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }

        // Các thuộc tính tương thích với code cũ
        [NotMapped]
        public bool IsActive => OrderStatus;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CancelReason { get; set; }

        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }

        public DateTime? PaidAt { get; set; }

        // Shipping Information
        public string Address { get; set; } = string.Empty;
        public string WardCode { get; set; } = string.Empty;
        public int DistrictId { get; set; }
        public string? ShippingOrderCode { get; set; }
        public string? ShippingNote { get; set; }
        public string? ShippingOrderResponse { get; set; }
    }
}