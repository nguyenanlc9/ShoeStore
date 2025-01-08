using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models.Payment;
using ShoeStore.Services.Momo;
using System.Text;
using System.Security.Cryptography;

namespace ShoeStore.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class MomoApiController : ControllerBase
    {
        private readonly IMomoService _momoService;

        public MomoApiController(IMomoService momoService)
        {
            _momoService = momoService;
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] OrderInfoModel model)
        {
            try
            {
                if (model.Amount <= 0)
                {
                    return BadRequest(new { message = "Số tiền phải lớn hơn 0" });
                }

                // Tạo mã đơn hàng test
                if (string.IsNullOrEmpty(model.OrderId))
                {
                    model.OrderId = $"TEST_{DateTime.Now:yyyyMMddHHmmss}";
                }

                // Tạo thông tin mặc định nếu chưa có
                if (string.IsNullOrEmpty(model.FullName))
                {
                    model.FullName = "Test User";
                }

                if (string.IsNullOrEmpty(model.OrderInfo))
                {
                    model.OrderInfo = $"Thanh toán đơn hàng test {model.OrderId}";
                }

                var response = await _momoService.CreatePaymentAsync(model);

                return Ok(new
                {
                    success = response.ErrorCode == 0,
                    data = response,
                    message = response.LocalMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("payment-callback")]
        public IActionResult PaymentCallback()
        {
            try
            {
                var queryString = HttpContext.Request.QueryString.ToString();
                var response = _momoService.PaymentExecuteAsync(Request.Query);

                return Ok(new
                {
                    success = true,
                    data = response,
                    queryString = queryString
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("verify-signature")]
        public IActionResult VerifySignature([FromBody] MomoSignatureModel model)
        {
            try
            {
                var rawHash = $"accessKey={model.AccessKey}" +
                             $"&amount={model.Amount}" +
                             $"&extraData=" +
                             $"&orderId={model.OrderId}" +
                             $"&orderInfo={model.OrderInfo}" +
                             $"&partnerCode={model.PartnerCode}" +
                             $"&requestId={model.RequestId}" +
                             $"&returnUrl={model.ReturnUrl}";

                var hash = ComputeHmacSha256(rawHash, model.SecretKey);

                return Ok(new
                {
                    success = true,
                    signature = hash,
                    rawHash = rawHash
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }

    public class MomoSignatureModel
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string PartnerCode { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public string OrderInfo { get; set; }
        public string ReturnUrl { get; set; }
        public long Amount { get; set; }
    }
} 