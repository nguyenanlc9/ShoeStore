using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Payment;
using ShoeStore.Services.ZaloPay;
using System.Text.Json;

namespace ShoeStore.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZaloPayTestController : ControllerBase
    {
        private readonly IZaloPayService _zaloPayService;
        private readonly ApplicationDbContext _context;

        public ZaloPayTestController(
            IZaloPayService zaloPayService,
            ApplicationDbContext context)
        {
            _zaloPayService = zaloPayService;
            _context = context;
        }

        [HttpPost("create-test-order")]
        public async Task<IActionResult> CreateTestOrder([FromBody] decimal amount)
        {
            try
            {
                var orderInfo = new OrderInfoModel
                {
                    OrderId = $"TEST_{DateTime.Now:yyyyMMddHHmmss}",
                    FullName = "Test User",
                    OrderInfo = "Test Payment",
                    Amount = (int)amount
                };

                var response = await _zaloPayService.CreatePaymentAsync(orderInfo);
                return Ok(new
                {
                    success = response.ReturnCode == 1,
                    data = response,
                    paymentUrl = response.OrderUrl,
                    message = response.ReturnMessage
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("query-transaction/{appTransId}")]
        public async Task<IActionResult> QueryTransaction(string appTransId)
        {
            try
            {
                var response = await _zaloPayService.QueryTransactionAsync(appTransId);
                return Ok(new
                {
                    success = true,
                    data = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("test-callback")]
        public async Task<IActionResult> TestCallback([FromBody] Dictionary<string, string> callbackData)
        {
            try
            {
                // Tạo query string giả lập từ callback data
                var queryParams = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
                var jsonData = JsonSerializer.Serialize(callbackData);
                queryParams.Add("data", jsonData);

                // Kiểm tra callback
                var isValid = await _zaloPayService.VerifyCallbackAsync(new QueryCollection(queryParams));

                return Ok(new
                {
                    success = true,
                    isValidCallback = isValid,
                    receivedData = callbackData
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("test-order/{orderId}")]
        public async Task<IActionResult> GetTestOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                return Ok(new
                {
                    success = true,
                    order = new
                    {
                        order.OrderId,
                        order.OrderCode,
                        order.TotalAmount,
                        order.PaymentStatus,
                        order.Status,
                        order.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}