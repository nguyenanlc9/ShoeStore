using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models.Payment;
using ShoeStore.Services.Momo;

namespace ShoeStore.Controllers
{
    public class TestMomoController : Controller
    {
        private readonly IMomoService _momoService;

        public TestMomoController(IMomoService momoService)
        {
            _momoService = momoService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTestPayment()
        {
            try
            {
                var orderInfo = new OrderInfoModel
                {
                    OrderId = $"TEST_{DateTime.Now.Ticks}",
                    Amount = 10000,
                    OrderInfo = "Thanh toan don hang test Momo",
                    FullName = "Test User"
                };

                var response = await _momoService.CreatePaymentAsync(orderInfo);
                
                if (response == null)
                {
                    return BadRequest(new { Message = "Không nhận được phản hồi từ Momo" });
                }

                if (response.ResultCode == 0)
                {
                    return Redirect(response.PayUrl);
                }

                return BadRequest(new { Message = $"Lỗi từ Momo: {response.Message} (Mã lỗi: {response.ResultCode})" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Lỗi xử lý: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult PaymentCallback()
        {
            try
            {
                var query = HttpContext.Request.Query;
                
                // Lấy các tham số từ query string
                var resultCode = query["resultCode"].ToString();
                var message = query["message"].ToString();
                var orderId = query["orderId"].ToString();

                // Kiểm tra kết quả thanh toán
                if (resultCode == "0")
                {
                    ViewBag.Message = $"Thanh toán thành công cho đơn hàng {orderId}";
                    return View("Success");
                }
                else
                {
                    ViewBag.Message = $"Thanh toán thất bại. Mã lỗi: {resultCode}. Lý do: {message}";
                    return View("Failure");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Có lỗi xảy ra trong quá trình xử lý thanh toán: {ex.Message}";
                return View("Failure");
            }
        }
    }
} 