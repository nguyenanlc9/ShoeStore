using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null)
                return RedirectToAction("Login", "Auth");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Size)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userInfo.UserID);

            if (order == null)
            {
                return NotFound();
            }

            return PartialView("_OrderDetails", order);
        }
    }
} 