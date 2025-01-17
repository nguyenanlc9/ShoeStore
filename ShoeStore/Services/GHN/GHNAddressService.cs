using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ShoeStore.Models.GHN;

namespace ShoeStore.Services.GHN
{
    public interface IGHNAddressService
    {
        Task<List<GHN_Province>> GetProvinces();
        Task<List<GHN_District>> GetDistricts(int provinceId);
        Task<List<GHN_Ward>> GetWards(int districtId);
    }

    public class GHNAddressService : IGHNAddressService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private const string BASE_URL = "https://dev-online-gateway.ghn.vn/shiip/public-api/master-data";

        public GHNAddressService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _token = configuration["GHN:Token"];
        }

        public async Task<List<GHN_Province>> GetProvinces()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/province");
                request.Headers.Add("Token", _token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"GHN API Error: {content}");
                }

                var result = JsonConvert.DeserializeObject<GHNResponse<List<GHN_Province>>>(content);
                return result?.Data ?? new List<GHN_Province>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting provinces: {ex.Message}");
            }
        }

        public async Task<List<GHN_District>> GetDistricts(int provinceId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/district");
                request.Headers.Add("Token", _token);

                var json = JsonConvert.SerializeObject(new { province_id = provinceId });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"GHN API Error: {content}");
                }

                var result = JsonConvert.DeserializeObject<GHNResponse<List<GHN_District>>>(content);
                return result?.Data ?? new List<GHN_District>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting districts: {ex.Message}");
            }
        }

        public async Task<List<GHN_Ward>> GetWards(int districtId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/ward");
                request.Headers.Add("Token", _token);

                var json = JsonConvert.SerializeObject(new { district_id = districtId });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"GHN API Error: {content}");
                }

                var result = JsonConvert.DeserializeObject<GHNResponse<List<GHN_Ward>>>(content);
                return result?.Data ?? new List<GHN_Ward>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting wards: {ex.Message}");
            }
        }

        private class GHNResponse<T>
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("data")]
            public T Data { get; set; }
        }
    }
} 