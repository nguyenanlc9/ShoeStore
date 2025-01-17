using Microsoft.AspNetCore.Mvc;
using ShoeStore.Services.GHN;

namespace ShoeStore.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly IGHNAddressService _addressService;

        public AddressController(IGHNAddressService addressService)
        {
            _addressService = addressService;
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
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            try
            {
                var districts = await _addressService.GetDistricts(provinceId);
                return Ok(districts);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            try
            {
                var wards = await _addressService.GetWards(districtId);
                return Ok(wards);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
} 