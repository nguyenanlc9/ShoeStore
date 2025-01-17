using ShoeStore.Models;

namespace ShoeStore.Services.GHN
{
    public interface IGHNService
    {
        Task<(bool Success, string OrderCode, string Message)> CreateShippingOrder(ShoeStore.Models.Order order, string wardCode, int districtId, int length = 20, int width = 20, int height = 10);
        Task<(bool Success, int Fee, string Message)> CalculateShippingFee(string toWardCode, int toDistrictId, int weight, int length = 20, int width = 20, int height = 10);
    }
} 