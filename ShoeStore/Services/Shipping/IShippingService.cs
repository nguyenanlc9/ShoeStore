using ShoeStore.Models;

namespace ShoeStore.Services.Shipping
{
    public interface IShippingService
    {
        Task<ShippingFeeResponse> CalculateShippingFee(ShippingFeeRequest request);
    }
} 