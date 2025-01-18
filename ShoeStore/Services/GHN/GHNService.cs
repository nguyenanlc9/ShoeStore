using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ShoeStore.Models;
using ShoeStore.Models.GHN;
using ShoeStore.Models.Enums;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ShoeStore.Services.GHN
{
    public class GHNService : IGHNService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly string _shopId;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GHNService> _logger;

        public GHNService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GHNService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _token = configuration["GHN:Token"];
            _shopId = configuration["GHN:ShopId"];

            _httpClient.BaseAddress = new Uri("https://dev-online-gateway.ghn.vn");
            _httpClient.DefaultRequestHeaders.Add("Token", _token);
            _httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);

            _logger = logger;
        }

        public async Task<(bool Success, string OrderCode, string Message)> CreateShippingOrder(Models.Order order, string wardCode, int districtId, int length = 20, int width = 20, int height = 10)
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

                _logger.LogInformation($"Create shipping order response: {responseContent}");

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
                _logger.LogError($"Error creating shipping order: {ex}");
                return (false, null, ex.Message);
            }
        }

        public async Task<(bool Success, decimal Total, string Message)> CalculateShippingFee(string toWardCode, int toDistrictId)
        {
            try
            {
                var fromDistrictId = int.Parse(_configuration["GHN:FromDistrictId"]);
                var fromWardCode = _configuration["GHN:FromWardCode"];
                var shopId = int.Parse(_configuration["GHN:ShopId"]);

                // Lấy danh sách dịch vụ khả dụng trước
                var availableServicesRequest = new
                {
                    shop_id = shopId,
                    from_district = fromDistrictId,
                    to_district = toDistrictId
                };

                var jsonServicesRequest = JsonSerializer.Serialize(availableServicesRequest);
                var servicesHttpContent = new StringContent(jsonServicesRequest, Encoding.UTF8, "application/json");

                var servicesResponse = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/available-services", servicesHttpContent);
                var servicesContent = await servicesResponse.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Available services response: {servicesContent}");

                if (!servicesResponse.IsSuccessStatusCode)
                {
                    return (false, 0, $"Error getting available services: {servicesContent}");
                }

                var servicesResult = JsonSerializer.Deserialize<GHNResponse<List<GHNServiceInfo>>>(servicesContent);
                if (servicesResult?.Code != 200 || servicesResult.Data == null || !servicesResult.Data.Any())
                {
                    return (false, 0, "Không có dịch vụ vận chuyển khả dụng cho địa chỉ này");
                }

                // Sử dụng service_id đầu tiên trong danh sách
                var serviceId = servicesResult.Data.First().ServiceId;

                var request = new
                {
                    from_district_id = fromDistrictId,
                    from_ward_code = fromWardCode,
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    service_id = serviceId,
                    shop_id = shopId,
                    weight = 200, // Giả sử trọng lượng mặc định là 200g
                    insurance_value = 0
                };

                _logger.LogInformation($"Calculating shipping fee with request: {JsonSerializer.Serialize(request)}");

                var jsonRequest = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/fee", httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Shipping fee API response: {responseContent}");

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
                _logger.LogError($"Error calculating shipping fee: {ex}");
                return (false, 0, ex.Message);
            }
        }

        public async Task<(bool Success, decimal Total, string Message)> CalculateShippingFeeDetail(string toWardCode, int toDistrictId, int weight, int length = 20, int width = 20, int height = 10)
        {
            try
            {
                var fromDistrictId = int.Parse(_configuration["GHN:FromDistrictId"]);
                var fromWardCode = _configuration["GHN:FromWardCode"];
                var shopId = int.Parse(_configuration["GHN:ShopId"]);

                var request = new
                {
                    from_district_id = fromDistrictId,
                    from_ward_code = fromWardCode,
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    service_id = 53320,
                    shop_id = shopId,
                    weight = weight,
                    length = length,
                    width = width,
                    height = height,
                    insurance_value = 0
                };

                _logger.LogInformation($"Calculating detailed shipping fee with request: {JsonSerializer.Serialize(request)}");

                var jsonRequest = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/fee", httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Detailed shipping fee API response: {responseContent}");

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
                _logger.LogError($"Error calculating detailed shipping fee: {ex}");
                return (false, 0, ex.Message);
            }
        }

        public async Task<(bool Success, int LeadTime, string Message)> GetLeadTime(
            int fromDistrictId,
            string fromWardCode,
            int toDistrictId,
            string toWardCode,
            int serviceId)
        {
            try
            {
                var request = new
                {
                    from_district_id = fromDistrictId,
                    from_ward_code = fromWardCode,
                    to_district_id = toDistrictId,
                    to_ward_code = toWardCode,
                    service_id = serviceId
                };

                _logger.LogInformation($"Calculating lead time with request: {JsonSerializer.Serialize(request)}");

                var jsonRequest = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/shiip/public-api/v2/shipping-order/leadtime", httpContent);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Lead time API response: {content}");

                var result = JsonSerializer.Deserialize<GHNResponse<GHNLeadTimeData>>(content);

                if (result?.Code == 200 && result.Data != null)
                {
                    return (true, result.Data.LeadTime, "Success");
                }

                _logger.LogWarning($"Failed to get lead time. Code: {result?.Code}, Message: {result?.Message}");
                return (false, 0, result?.Message ?? "Không thể tính thời gian giao hàng");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating lead time: {ex}");
                return (false, 0, ex.Message);
            }
        }

        public class GHNLeadTimeData
        {
            [JsonPropertyName("leadtime")]
            public int LeadTime { get; set; }
            
            [JsonPropertyName("order_date")]
            public string OrderDate { get; set; }
        }
    }
} 