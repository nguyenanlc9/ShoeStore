using ShoeStore.Models.Address;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShoeStore.Services.APIAddress
{
    public class AddressService : IAddressService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://provinces.open-api.vn/api";
        private readonly JsonSerializerOptions _jsonOptions;

        public AddressService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<List<Province>> GetProvinces()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/p/");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                
                // Debug log
                Console.WriteLine($"API Response: {content}");
                
                return JsonSerializer.Deserialize<List<Province>>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                // Debug log
                Console.WriteLine($"Error getting provinces: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<District>> GetDistricts(int provinceCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/p/{provinceCode}?depth=2");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var provinceData = JsonSerializer.Deserialize<ProvinceDetail>(content, _jsonOptions);
                return provinceData.Districts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting districts: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Ward>> GetWards(int districtCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/d/{districtCode}?depth=2");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var districtData = JsonSerializer.Deserialize<DistrictDetail>(content, _jsonOptions);
                return districtData.Wards;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting wards: {ex.Message}");
                throw;
            }
        }
    }

    public class ProvinceDetail : Province
    {
        [JsonPropertyName("districts")]
        public List<District> Districts { get; set; }
    }

    public class DistrictDetail : District
    {
        [JsonPropertyName("wards")]
        public List<Ward> Wards { get; set; }
    }
}