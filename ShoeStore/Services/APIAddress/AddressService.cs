using System.Net.Http.Json;
using ShoeStore.Models.APIAddress;

namespace ShoeStore.Services.APIAddress
{
    public class AddressService : IAddressService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://provinces.open-api.vn/api";

        public AddressService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Province>> GetProvinces()
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/p/");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Province>>();
        }

        public async Task<List<District>> GetDistricts(int provinceCode)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/p/{provinceCode}?depth=2");
            response.EnsureSuccessStatusCode();
            var province = await response.Content.ReadFromJsonAsync<Province>();
            return province.Districts;
        }

        public async Task<List<Ward>> GetWards(int districtCode)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/d/{districtCode}?depth=2");
            response.EnsureSuccessStatusCode();
            var district = await response.Content.ReadFromJsonAsync<District>();
            return district.Wards;
        }

        public async Task<string> GetProvinceName(int provinceCode)
        {
            var provinces = await GetProvinces();
            return provinces.FirstOrDefault(p => p.Code == provinceCode)?.Name ?? "";
        }

        public async Task<string> GetDistrictName(int districtCode)
        {
            var provinces = await GetProvinces();
            foreach (var province in provinces)
            {
                var districts = await GetDistricts(province.Code);
                var district = districts.FirstOrDefault(d => d.Code == districtCode);
                if (district != null)
                {
                    return district.Name;
                }
            }
            return "";
        }

        public async Task<string> GetWardName(int wardCode)
        {
            var provinces = await GetProvinces();
            foreach (var province in provinces)
            {
                var districts = await GetDistricts(province.Code);
                foreach (var district in districts)
                {
                    var wards = await GetWards(district.Code);
                    var ward = wards.FirstOrDefault(w => w.Code == wardCode);
                    if (ward != null)
                    {
                        return ward.Name;
                    }
                }
            }
            return "";
        }
    }
} 