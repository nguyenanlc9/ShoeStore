using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ShoeStore.Services.GHN;
using ShoeStore.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace ShoeStore.Controllers
{
    [Route("api/ghn")]
    [ApiController]
    public class ShippingController : Controller
    {
        private readonly IGHNService _ghnService;
        private readonly IGHNAddressService _ghnAddressService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(IGHNService ghnService, IGHNAddressService ghnAddressService, IConfiguration configuration, ILogger<ShippingController> logger)
        {
            _ghnService = ghnService;
            _ghnAddressService = ghnAddressService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var provinces = await _ghnAddressService.GetProvinces();
            return Ok(provinces);
        }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await _ghnAddressService.GetDistricts(provinceId);
            return Ok(districts);
        }

        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await _ghnAddressService.GetWards(districtId);
            return Ok(wards);
        }

        [HttpGet("calculate-fee")]
        public async Task<IActionResult> CalculateShippingFee([FromQuery] string wardCode, [FromQuery] int districtId)
        {
            try
            {
                var (success, shippingFee, message) = await _ghnService.CalculateShippingFee(wardCode, districtId);
                if (!success)
                {
                    return BadRequest(new { success = false, message = message });
                }

                // Lấy thời gian giao hàng dự kiến
                var fromDistrictId = int.Parse(_configuration["GHN:FromDistrictId"]);
                var fromWardCode = _configuration["GHN:FromWardCode"];
                var serviceId = 53320; // Hoặc lấy từ configuration

                var (leadTimeSuccess, leadTime, leadTimeMessage) = await _ghnService.GetLeadTime(
                    fromDistrictId,
                    fromWardCode,
                    districtId,
                    wardCode,
                    serviceId
                );

                if (!leadTimeSuccess)
                {
                    _logger.LogWarning($"Failed to get lead time: {leadTimeMessage}");
                }

                // Tính ngày giao hàng dự kiến
                var expectedDelivery = "";
                if (leadTimeSuccess)
                {
                    var startDate = DateTime.Now.AddDays(leadTime - 1);
                    var endDate = DateTime.Now.AddDays(leadTime);
                    expectedDelivery = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}";
                }

                return Ok(new
                {
                    success = true,
                    shippingFee = shippingFee,
                    expectedDelivery = expectedDelivery
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calculating shipping fee: {ex}");
                return BadRequest(new { success = false, message = "Lỗi khi tính phí vận chuyển" });
            }
        }
    }

    public class ShippingFeeRequest
    {
        public string ToWardCode { get; set; }
        public int ToDistrictId { get; set; }
    }
} 