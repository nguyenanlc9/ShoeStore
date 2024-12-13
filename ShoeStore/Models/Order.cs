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
        public string? OrderUsName { get; set; }
        public string? OrderCode { get; set; }
        public string? OrderDescription { get; set; }
        public string? OrderCoupon { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public bool OrderStatus { get; set; }
    }
}