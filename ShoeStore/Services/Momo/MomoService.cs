using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShoeStore.Models.Payment;
using ShoeStore.Models.Payment.Momo;

namespace ShoeStore.Services.Momo
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        private readonly HttpClient _httpClient;

        public MomoService(IOptions<MomoOptionModel> options, HttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
        }

        public async Task<MomoCreatePaymentResponse> CreatePaymentAsync(OrderInfoModel model)
        {
            try
            {
                var request = new MomoCreatePaymentRequest
                {
                    PartnerCode = _options.Value.PartnerCode,
                    AccessKey = _options.Value.AccessKey,
                    RequestId = DateTime.UtcNow.Ticks.ToString(),
                    Amount = model.Amount,
                    OrderId = model.OrderId,
                    OrderInfo = model.OrderInfo,
                    RedirectUrl = _options.Value.ReturnUrl,
                    IpnUrl = _options.Value.NotifyUrl,
                    RequestType = "captureWallet",
                    ExtraData = "",
                    Lang = "vi"
                };

                var rawData = $"accessKey={_options.Value.AccessKey}" +
                             $"&amount={request.Amount}" +
                             $"&extraData={request.ExtraData}" +
                             $"&ipnUrl={request.IpnUrl}" +
                             $"&orderId={request.OrderId}" +
                             $"&orderInfo={request.OrderInfo}" +
                             $"&partnerCode={request.PartnerCode}" +
                             $"&redirectUrl={request.RedirectUrl}" +
                             $"&requestId={request.RequestId}" +
                             $"&requestType={request.RequestType}";

                request.Signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

                var jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_options.Value.PaymentEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Request: {jsonRequest}");
                Console.WriteLine($"Response: {responseContent}");

                return JsonSerializer.Deserialize<MomoCreatePaymentResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Momo payment: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> PaymentExecuteAsync(Dictionary<string, string> queryParams)
        {
            try
            {
                var rawSignature = queryParams["signature"];
                var rawData = BuildRawHash(queryParams);
                var checkSignature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

                Console.WriteLine("Raw Data: " + rawData);
                Console.WriteLine("Momo Signature: " + rawSignature);
                Console.WriteLine("Our Signature: " + checkSignature);

                return rawSignature.Equals(checkSignature);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing Momo payment: {ex.Message}");
                return false;
            }
        }

        private string BuildRawHash(Dictionary<string, string> queryParams)
        {
            var signatureRaw = string.Join("&", new[] {
                $"accessKey={_options.Value.AccessKey}",
                $"amount={queryParams["amount"]}",
                $"extraData={queryParams.GetValueOrDefault("extraData", "")}",
                $"message={queryParams["message"]}",
                $"orderId={queryParams["orderId"]}",
                $"orderInfo={queryParams["orderInfo"]}",
                $"orderType={queryParams.GetValueOrDefault("orderType", "momo_wallet")}",
                $"partnerCode={queryParams["partnerCode"]}",
                $"payType={queryParams.GetValueOrDefault("payType", "")}", 
                $"requestId={queryParams.GetValueOrDefault("requestId", "")}",
                $"responseTime={queryParams.GetValueOrDefault("responseTime", "")}",
                $"resultCode={queryParams["resultCode"]}",
                $"transId={queryParams["transId"]}"
            });

            Console.WriteLine($"AccessKey: {_options.Value.AccessKey}");
            Console.WriteLine($"SecretKey: {_options.Value.SecretKey}");
            return signatureRaw;
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hashString;
            }
        }
    }
} 