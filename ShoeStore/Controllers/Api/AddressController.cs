using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models.Address;
using Microsoft.Extensions.Logging;
using ShoeStore.Services.APIAddress;

namespace ShoeStore.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;
        private readonly ILogger<AddressController> _logger;

        public AddressController(IAddressService addressService, ILogger<AddressController> logger)
        {
            _addressService = addressService;
            _logger = logger;
        }

        [HttpGet("provinces")]
        public async Task<ActionResult<List<Province>>> GetProvinces()
        {
            try
            {
                _logger.LogInformation("Getting provinces...");
                var provinces = await _addressService.GetProvinces();
                return Ok(provinces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provinces");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("districts/{provinceCode}")]
        public async Task<ActionResult<List<District>>> GetDistricts(int provinceCode)
        {
            try
            {
                _logger.LogInformation($"Getting districts for province {provinceCode}");
                var districts = await _addressService.GetDistricts(provinceCode);
                return Ok(districts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting districts for province {provinceCode}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("wards/{districtCode}")]
        public async Task<ActionResult<List<Ward>>> GetWards(int districtCode)
        {
            try
            {
                _logger.LogInformation($"Getting wards for district {districtCode}");
                var wards = await _addressService.GetWards(districtCode);
                return Ok(wards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting wards for district {districtCode}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
} 