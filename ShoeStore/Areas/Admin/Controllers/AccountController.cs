using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Areas.Admin.DTOs.Request;
using Microsoft.AspNetCore.Http;
using ShoeStore.Utils;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            var login = Request.Cookies.Get<LoginDTO>("UserCredential");
            if (login != null)
            {
                var result = _context.Users.AsNoTracking()
                    .FirstOrDefault(x => x.Username == login.UserName &&
                            x.Password == login.Password);
                if (result != null)
                {
                    HttpContext.Session.Set<User>("userInfo", result);
                    return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDTO login)
        {
            var result = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == login.UserName &&
                    x.Password == login.Password);

            if (result != null)
            {
                if (result.Role.RoleName == "Admin")
                {
                    if (login.RememberMe)
                    {
                        Response.Cookies.Append("UserCredential", login, new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(7),
                            HttpOnly = true,
                            IsEssential = true
                        });
                    }

                    HttpContext.Session.Set<User>("userInfo", result);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewData["Message"] = "Bạn không có quyền truy cập!";
                }
            }
            else
            {
                ViewData["Message"] = "Tài khoản hoặc mật khẩu không đúng!";
            }
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserCredential");
            return RedirectToAction("Login");
        }
    }
}
