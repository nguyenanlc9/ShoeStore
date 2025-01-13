using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Data;
using ShoeStore.Services.APIAddress;

namespace ShoeStore.Services.Order
{
    public class ShippingService : IShippingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAddressService _addressService;

        public ShippingService(ApplicationDbContext context, IAddressService addressService)
        {
            _context = context;
            _addressService = addressService;
        }

        public async Task<decimal> CalculateShippingFee(string province, decimal orderAmount)
        {
            try
            {
                // Lấy thông tin tỉnh/thành phố từ API
                var provinces = await _addressService.GetProvinces();
                var provinceInfo = provinces.FirstOrDefault(p => p.Name.ToLower().Contains(province.ToLower()));

                if (provinceInfo == null)
                    return 0;

                // Lấy phí vận chuyển từ database
                var rate = await GetShippingRateByProvince(province);
                if (rate == null || !rate.IsActive)
                    return 0;

                decimal baseFee = rate.BaseFee;

                // Giảm phí vận chuyển cho đơn hàng có giá trị cao
                if (orderAmount >= 1000000) // Trên 1 triệu
                {
                    baseFee *= 0.8m; // Giảm 20%
                }
                else if (orderAmount >= 500000) // Trên 500k
                {
                    baseFee *= 0.9m; // Giảm 10%
                }

                return baseFee;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating shipping fee: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<ShippingRate>> GetAllShippingRates()
        {
            return await _context.ShippingRates
                .OrderBy(r => r.Province)
                .ToListAsync();
        }

        public async Task<ShippingRate> GetShippingRateByProvince(string province)
        {
            return await _context.ShippingRates
                .FirstOrDefaultAsync(r => r.Province.ToLower().Contains(province.ToLower()) && r.IsActive);
        }

        public async Task<ShippingRate> AddShippingRate(ShippingRate rate)
        {
            // Kiểm tra tỉnh/thành phố có tồn tại trong API không
            var provinces = await _addressService.GetProvinces();
            var provinceExists = provinces.Any(p => p.Name.ToLower().Contains(rate.Province.ToLower()));

            if (!provinceExists)
            {
                throw new Exception("Tỉnh/thành phố không hợp lệ");
            }

            rate.CreatedAt = DateTime.Now;
            _context.ShippingRates.Add(rate);
            await _context.SaveChangesAsync();
            return rate;
        }

        public async Task<ShippingRate> UpdateShippingRate(ShippingRate rate)
        {
            // Kiểm tra tỉnh/thành phố có tồn tại trong API không
            var provinces = await _addressService.GetProvinces();
            var provinceExists = provinces.Any(p => p.Name.ToLower().Contains(rate.Province.ToLower()));

            if (!provinceExists)
            {
                throw new Exception("Tỉnh/thành phố không hợp lệ");
            }

            rate.UpdatedAt = DateTime.Now;
            _context.Entry(rate).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return rate;
        }

        public async Task DeleteShippingRate(int id)
        {
            var rate = await _context.ShippingRates.FindAsync(id);
            if (rate != null)
            {
                _context.ShippingRates.Remove(rate);
                await _context.SaveChangesAsync();
            }
        }
    }
} 