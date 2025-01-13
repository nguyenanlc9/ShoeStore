using System.Text.Json.Serialization;

namespace ShoeStore.Models.Payment.Momo
{
    public class MomoCreatePaymentRequest
    {
        [JsonPropertyName("partnerCode")]
        public string PartnerCode { get; set; }

        [JsonPropertyName("accessKey")]
        public string AccessKey { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; }

        [JsonPropertyName("orderInfo")]
        public string OrderInfo { get; set; }

        [JsonPropertyName("redirectUrl")]
        public string RedirectUrl { get; set; }

        [JsonPropertyName("ipnUrl")]
        public string IpnUrl { get; set; }

        [JsonPropertyName("requestType")]
        public string RequestType { get; set; }

        [JsonPropertyName("extraData")]
        public string ExtraData { get; set; }

        [JsonPropertyName("lang")]
        public string Lang { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }
} 