namespace ShoeStore.Models.Payment
{
    public class VNPayInformationModel
    {
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; }
        public string Name { get; set; }
        public string OrderType { get; set; }
        public string OrderId { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }
} 