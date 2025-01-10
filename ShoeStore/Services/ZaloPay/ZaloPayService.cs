using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShoeStore.Models.Payment;

namespace ShoeStore.Services.ZaloPay
{
    public class ZaloPayService : IZaloPayService
    {
        private readonly ZaloPayConfig _config;
        private readonly HttpClient _httpClient;

        public ZaloPayService(IOptions<ZaloPayConfig> config, HttpClient httpClient)
        {
            _config = config.Value;
            _httpClient = httpClient;
        }

        public async Task<ZaloPayCreateOrderResponse> CreatePaymentAsync(OrderInfoModel model)
        {
            try
            {
                var embedData = new Dictionary<string, string>
                {
                    { "merchantinfo", "ShoeStore" },
                    { "redirecturl", _config.RedirectUrl }
                };

                var items = new[] { new { itemid = "knb", itemname = model.OrderInfo, itemprice = model.Amount, itemquantity = 1 } };

                var param = new Dictionary<string, string>
                {
                    { "appid", _config.AppId },
                    { "apptime", DateTimeOffset.Now.ToUnixTimeSeconds().ToString() },
                    { "apptransid", DateTime.Now.ToString("yyMMdd") + "_" + model.OrderId },
                    { "appuser", model.FullName },
                    { "embeddata", JsonSerializer.Serialize(embedData) },
                    { "item", JsonSerializer.Serialize(items) },
                    { "description", $"ShoeStore - Thanh toán đơn hàng #{model.OrderId}" },
                    { "amount", model.Amount.ToString() },
                    { "bankcode", "zalopayapp" }
                };

                param.Add("mac", GetMacCreateOrder(param));

                var content = new FormUrlEncodedContent(param);
                var response = await _httpClient.PostAsync(_config.CreateOrderUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ZaloPayCreateOrderResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating ZaloPay payment: {ex.Message}");
                throw;
            }
        }

        public async Task<ZaloPayQueryResponse> QueryTransactionAsync(string appTransId)
        {
            try
            {
                var param = new Dictionary<string, string>
                {
                    { "appid", _config.AppId },
                    { "apptransid", appTransId }
                };

                var rawHash = string.Join("|", param.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                            .OrderBy(kv => kv.Key)
                                            .Select(kv => $"{kv.Key}={kv.Value}"));
                var mac = HmacSHA256(rawHash, _config.Key1);
                param.Add("mac", mac);

                var content = new FormUrlEncodedContent(param);
                var response = await _httpClient.PostAsync(_config.QueryOrderUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<ZaloPayQueryResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying ZaloPay transaction: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VerifyCallbackAsync(IQueryCollection collection)
        {
            try
            {
                var dataStr = collection["data"].ToString();
                var mac = collection["mac"].ToString();

                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(dataStr);
                var calcMac = GetMacCallback(data);

                return mac == calcMac;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying ZaloPay callback: {ex.Message}");
                return false;
            }
        }

        public string GetMacCreateOrder(Dictionary<string, string> data)
        {
            var rawHash = string.Join("|", data.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                            .OrderBy(kv => kv.Key)
                                            .Select(kv => $"{kv.Key}={kv.Value}"));
            return HmacSHA256(rawHash, _config.Key1);
        }

        public string GetMacCallback(Dictionary<string, string> data)
        {
            var rawHash = string.Join("|", data.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                            .OrderBy(kv => kv.Key)
                                            .Select(kv => $"{kv.Key}={kv.Value}"));
            return HmacSHA256(rawHash, _config.Key2);
        }

        private string HmacSHA256(string message, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
} 