using ShoeStore.Models;
using System.Threading.Tasks;

namespace ShoeStore.Services.GHN
{
    public interface IGHNService
    {
        Task<(bool Success, string OrderCode, string Message)> CreateShippingOrder(Models.Order order, string wardCode, int districtId, int length = 20, int width = 20, int height = 10);
        
        // Phương thức tính phí vận chuyển đơn giản cho khách hàng
        Task<(bool Success, decimal Total, string Message)> CalculateShippingFee(string toWardCode, int toDistrictId);
        
        // Phương thức tính phí vận chuyển chi tiết cho admin
        Task<(bool Success, decimal Total, string Message)> CalculateShippingFeeDetail(string toWardCode, int toDistrictId, int weight, int length = 20, int width = 20, int height = 10);
        
        Task<(bool Success, int LeadTime, string Message)> GetLeadTime(
            int fromDistrictId,
            string fromWardCode,
            int toDistrictId, 
            string toWardCode,
            int serviceId);
    }
} 