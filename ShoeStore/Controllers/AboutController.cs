using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Controllers
{
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AboutController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var about = await _context.Abouts
                .Where(a => a.Status == 1)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return View(about);
        }
    }
} 