using Newtonsoft.Json;
using System.Text.Json.Serialization;

public class MomoCreatePaymentResponseModel
{
    [JsonProperty("requestId")]
    public string RequestId { get; set; }

    [JsonProperty("errorCode")]
    public int ErrorCode { get; set; }

    [JsonProperty("orderId")]
    public string OrderId { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("localMessage")]
    public string LocalMessage { get; set; }

    [JsonProperty("requestType")]
    public string RequestType { get; set; }

    [JsonProperty("payUrl")]
    public string PayUrl { get; set; }

    [JsonProperty("signature")]
    public string Signature { get; set; }

    [JsonProperty("qrCodeUrl")]
    public string QrCodeUrl { get; set; }

    [JsonProperty("deeplink")]
    public string Deeplink { get; set; }

    [JsonProperty("deeplinkWebInApp")]
    public string DeeplinkWebInApp { get; set; }

    [JsonProperty("resultCode")]
    public int ResultCode { get; set; }
} 