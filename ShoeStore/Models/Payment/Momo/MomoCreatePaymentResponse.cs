using System.Text.Json.Serialization;

namespace ShoeStore.Models.Payment.Momo
{
    public class MomoCreatePaymentResponse
    {
        [JsonPropertyName("partnerCode")]
        public string PartnerCode { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("responseTime")]
        public long ResponseTime { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("payUrl")]
        public string PayUrl { get; set; }

        [JsonPropertyName("deeplink")]
        public string Deeplink { get; set; }

        [JsonPropertyName("qrCodeUrl")]
        public string QrCodeUrl { get; set; }
    }
} 