using ShoeStore.Models;

namespace ShoeStore.Services.Order
{
    public interface IShippingService
    {
        Task<decimal> CalculateShippingFee(string province, decimal orderAmount);
        Task<List<ShippingRate>> GetAllShippingRates();
        Task<ShippingRate> GetShippingRateByProvince(string province);
        Task<ShippingRate> AddShippingRate(ShippingRate rate);
        Task<ShippingRate> UpdateShippingRate(ShippingRate rate);
        Task DeleteShippingRate(int id);
    }
} 