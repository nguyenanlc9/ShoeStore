namespace ShoeStore.Models.Payment.Momo
{
    public class MomoSignatureModel
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string PartnerCode { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public string OrderInfo { get; set; }
        public string ReturnUrl { get; set; }
        public long Amount { get; set; }
    }
}
