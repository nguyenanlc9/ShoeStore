using ShoeStore.Models.Address;

namespace ShoeStore.Services.APIAddress
{
    public interface IAddressService
    {
        Task<List<Province>> GetProvinces();
        Task<List<District>> GetDistricts(int provinceCode);
        Task<List<Ward>> GetWards(int districtCode);
    }
}