using System.Text.Json.Serialization;

namespace ShoeStore.Models.GHN
{
    public class GHNResponse<T>
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    public class GHNOrderResult
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; }

        [JsonPropertyName("expected_delivery_time")]
        public string ExpectedDeliveryTime { get; set; }

        [JsonPropertyName("fee")]
        public GHNFee Fee { get; set; }
    }

    public class GHNFee
    {
        [JsonPropertyName("main_service")]
        public int MainService { get; set; }

        [JsonPropertyName("insurance")]
        public int Insurance { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
} 