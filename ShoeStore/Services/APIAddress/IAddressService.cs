using ShoeStore.Models.APIAddress;

namespace ShoeStore.Services.APIAddress
{
    public interface IAddressService
    {
        Task<List<Province>> GetProvinces();
        Task<List<District>> GetDistricts(int provinceCode);
        Task<List<Ward>> GetWards(int districtCode);
        Task<string> GetProvinceName(int provinceCode);
        Task<string> GetDistrictName(int districtCode);
        Task<string> GetWardName(int wardCode);
    }
} 