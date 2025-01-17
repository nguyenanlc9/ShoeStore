using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ShoeStore.Models;
using ShoeStore.Models.GHN;
using ShoeStore.Models.Enums;

namespace ShoeStore.Services.GHN
{
    public class GHNService : IGHNService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly string _shopId;
        private readonly IConfiguration _configuration;

        public GHNService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _token = configuration["GHN:Token"];
            _shopId = configuration["GHN:ShopId"];

            _httpClient.BaseAddress = new Uri("https://dev-online-gateway.ghn.vn");
            _httpClient.DefaultRequestHeaders.Add("Token", _token);
            _httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);
        }

        public async Task<(bool Success, string OrderCode, string Message)> CreateShippingOrder(ShoeStore.Models.Order order, string wardCode, int districtId, int length = 20, int width = 20, int height = 10)
        {
            try
            {
                // Kiểm tra cấu hình GHN
                var fromDistrictId = _configuration["GHN:FromDistrictId"];
                if (string.IsNullOrEmpty(fromDistrictId))
                {
                    return (false, null, "Thiếu cấu hình FromDistrictId trong GHN");
                }

                var items = order.OrderDetails.Select(od => new
                {
                    name = od.Product.Name,
                    quantity = od.Quantity,
                    weight = od.Product.Weight
                }).ToList();

                var totalWeight = items.Sum(i => i.weight * i.quantity);

                var requestBody = new
                {
                    payment_type_id = 2,
                    note = "",
                    required_note = "KHONGCHOXEMHANG",
                    from_name = _configuration["GHN:FromName"],
                    from_phone = _configuration["GHN:FromPhone"],
                    from_address = _configuration["GHN:FromAddress"],
                    from_ward_code = _configuration["GHN:FromWardCode"],
                    from_district_id = int.Parse(fromDistrictId),
                    return_phone = _configuration["GHN:FromPhone"],
                    return_address = _configuration["GHN:FromAddress"],
                    return_district_id = int.Parse(fromDistrictId),
                    return_ward_code = _configuration["GHN:FromWardCode"],
                    client_order_code = order.OrderCode,
                    to_name = order.OrderUsName,
                    to_phone = order.PhoneNumber,
                    to_address = order.ShippingAddress,
                    to_ward_code = wardCode,
                    to_district_id = districtId,
                    cod_amount = order.PaymentMethod == PaymentMethod.COD ? (int)order.TotalAmount : 0,
                    content = $"Đơn hàng {order.OrderCode}",
                    weight = totalWeight,
                    length = length,
                    width = width,
                    height = height,
                    service_type_id = 2,
                    service_id = 0,
                    items = items
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/create", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GHNResponse<GHNOrderResult>>(responseContent);
                    if (result?.Code == 200)
                    {
                        return (true, result.Data.OrderCode, null);
                    }
                    return (false, null, result?.Message ?? "Lỗi không xác định từ GHN");
                }

                return (false, null, $"Lỗi từ GHN: {responseContent}");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, int Fee, string Message)> CalculateShippingFee(string toWardCode, int toDistrictId, int weight, int length = 20, int width = 20, int height = 10)
        {
            try
            {
                var fromDistrictId = _configuration["GHN:FromDistrictId"];
                if (string.IsNullOrEmpty(fromDistrictId))
                {
                    return (false, 0, "Thiếu cấu hình FromDistrictId trong GHN");
                }

                var request = new
                {
                    shop_id = int.Parse(_shopId),
                    from_district_id = int.Parse(fromDistrictId),
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    weight = weight,
                    length = length,
                    width = width,
                    height = height,
                    service_type_id = 2
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/fee", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<GHNResponse<GHNFee>>(responseContent);
                    if (result.Code == 200)
                    {
                        return (true, result.Data.Total, "Success");
                    }
                    return (false, 0, result.Message);
                }

                return (false, 0, $"Error: {response.StatusCode} - {responseContent}");
            }
            catch (Exception ex)
            {
                return (false, 0, ex.Message);
            }
        }
    }
} 