using Microsoft.AspNetCore.Mvc;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "dashboard";
            return View();
        }
    }
}
