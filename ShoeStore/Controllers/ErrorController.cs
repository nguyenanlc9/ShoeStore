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
                    ViewBag.ErrorMessage = "Không tìm thấy trang bạn yêu cầu";
                    ViewBag.ErrorCode = "404";
                    ViewBag.ErrorTitle = "Trang không tồn tại";
                    break;
                default:
                    ViewBag.ErrorMessage = "Đã xảy ra lỗi trong quá trình xử lý";
                    ViewBag.ErrorCode = statusCode;
                    ViewBag.ErrorTitle = "Lỗi";
                    break;
            }
            return View("Error");
        }
    }
} 