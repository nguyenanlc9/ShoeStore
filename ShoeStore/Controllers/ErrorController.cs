using Microsoft.AspNetCore.Mvc;

namespace ShoeStore.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Không tìm thấy trang yêu cầu";
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Có lỗi xảy ra từ máy chủ";
                    break;
                default:
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi";
                    break;
            }
            return View("Error");
        }

        public IActionResult PaymentError(string message)
        {
            ViewBag.ErrorMessage = message ?? "Có lỗi xảy ra trong quá trình thanh toán";
            return View();
        }
    }
} 