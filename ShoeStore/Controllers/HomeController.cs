using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using System.Diagnostics;

namespace Project_BE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Index()
        {
            ViewData["HotContact"] = _context.Contacts
                .AsNoTracking()
                .OrderBy(x => x.ContactName)
                .ToList();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }


    }
}
