using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.ViewComponents
{
    public class FooterViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public FooterViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var footerInfo = await _context.Footers.FirstOrDefaultAsync();
            ViewData["FooterInfo"] = footerInfo;
            return View("Default");
        }
    }
} 