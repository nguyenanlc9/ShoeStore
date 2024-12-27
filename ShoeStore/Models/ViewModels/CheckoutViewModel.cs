using ShoeStore.Models.Enums;

namespace ShoeStore.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
    }
} 