using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CartCountViewComponent(ApplicationDbContext context)
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

            var cartCount = await _context.CartItems
                .Where(ci => ci.UserId == userInfo.UserID)
                .SumAsync(ci => ci.Quantity);

            return View(cartCount);
        }
    }
} 