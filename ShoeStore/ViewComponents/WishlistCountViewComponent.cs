using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.ViewComponents
{
    public class WishlistCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public WishlistCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
            {
                return View(0);
            }

            var count = await _context.Wishlists.CountAsync(w => w.UserId == userInfo.UserID);
            return View(count);
        }
    }
} 