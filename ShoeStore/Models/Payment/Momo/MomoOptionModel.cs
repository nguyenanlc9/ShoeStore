using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.Payment.Momo
{
    public class MomoOptionModel
    {
        public string PartnerCode { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string PaymentEndpoint { get; set; }
        public string ReturnUrl { get; set; }
        public string NotifyUrl { get; set; }
        public string RequestType { get; set; } = "captureWallet";
        public string Language { get; set; } = "vi";
    }
} 