using Microsoft.AspNetCore.Mvc;
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

        public IViewComponentResult Invoke()
        {
            var footerInfo = _context.Footers.OrderByDescending(f => f.FooterId).FirstOrDefault();
            return View(footerInfo);
        }
    }
} 