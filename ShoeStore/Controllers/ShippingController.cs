using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models;
using ShoeStore.Services.Order;
using ShoeStore.Services.APIAddress;

namespace ShoeStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingService _shippingService;
        private readonly IAddressService _addressService;

        public ShippingController(IShippingService shippingService, IAddressService addressService)
        {
            _shippingService = shippingService;
            _addressService = addressService;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateShippingFee([FromBody] ShippingFeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Lấy thông tin tỉnh/thành phố từ API
                var provinces = await _addressService.GetProvinces();
                var province = provinces.FirstOrDefault(p => p.Code == int.Parse(request.Province));

                if (province == null)
                {
                    return BadRequest(new { success = false, message = "Tỉnh/thành phố không hợp lệ" });
                }

                // Lấy thông tin quận/huyện
                var districts = await _addressService.GetDistricts(int.Parse(request.Province));
                var district = districts.FirstOrDefault(d => d.Code == int.Parse(request.District));

                if (district == null)
                {
                    return BadRequest(new { success = false, message = "Quận/huyện không hợp lệ" });
                }

                // Lấy thông tin phường/xã
                var wards = await _addressService.GetWards(int.Parse(request.District));
                var ward = wards.FirstOrDefault(w => w.Code == int.Parse(request.Ward));

                if (ward == null)
                {
                    return BadRequest(new { success = false, message = "Phường/xã không hợp lệ" });
                }

                // Tính phí vận chuyển
                var shippingFee = await _shippingService.CalculateShippingFee(province.Name, request.OrderAmount);

                // Lấy thông tin phí vận chuyển từ database
                var shippingRate = await _shippingService.GetShippingRateByProvince(province.Name);

                var response = new ShippingFeeResponse
                {
                    Success = true,
                    ShippingFee = shippingFee,
                    EstimatedDeliveryTime = shippingRate?.DeliveryDays + "-" + (shippingRate?.DeliveryDays + 1) + " ngày",
                    Message = "Tính phí vận chuyển thành công"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            try
            {
                var provinces = await _addressService.GetProvinces();
                return Ok(provinces);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("districts/{provinceCode}")]
        public async Task<IActionResult> GetDistricts(int provinceCode)
        {
            try
            {
                var districts = await _addressService.GetDistricts(provinceCode);
                return Ok(districts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("wards/{districtCode}")]
        public async Task<IActionResult> GetWards(int districtCode)
        {
            try
            {
                var wards = await _addressService.GetWards(districtCode);
                return Ok(wards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
} 