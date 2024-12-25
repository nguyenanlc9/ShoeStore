using Microsoft.AspNetCore.Mvc;
using ShoeStore.Filters;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "dashboard";
            return View();
        }

    }
}
