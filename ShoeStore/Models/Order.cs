using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    public enum PaymentMethod
    {
        Cash = 1,
        CreditCard = 2,
        Online = 3
    }
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        [StringLength(50, ErrorMessage = "Tên sản phẩm không được vượt quá 50 ký tự.")]
        public string? OrderUsName { get; set; }
        public string? OrderCode { get; set; }
        public string? OrderDescription { get; set; }
        public string? OrderCoupon { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public bool OrderStatus { get; set; }
    }
}