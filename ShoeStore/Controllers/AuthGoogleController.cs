using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.Controllers
{
    public class AuthGoogleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthGoogleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AuthGoogle/Login
        public IActionResult Login()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", "AuthGoogle")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: /AuthGoogle/GoogleResponse
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return RedirectToAction("Login", "Auth");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Tạo username duy nhất từ email
                string baseUsername = email.Split('@')[0];
                string username = baseUsername;
                int counter = 1;
                
                // Kiểm tra xem username đã tồn tại chưa
                while (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    username = $"{baseUsername}{counter}";
                    counter++;
                }

                // Tạo tài khoản mới nếu chưa tồn tại
                user = new User
                {
                    Email = email,
                    Username = username,
                    FullName = result.Principal.FindFirstValue(ClaimTypes.Name),
                    PasswordHash = "", // Không cần mật khẩu cho tài khoản Google
                    RoleID = 1, // Role mặc định là User
                    Status = true,
                    EmailConfirmed = true, // Email Google đã được xác thực
                    RegisterDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    Phone = "Chưa cập nhật" // Đặt giá trị mặc định cho Phone
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Lưu thông tin user vào session
            HttpContext.Session.Set<User>("userInfo", user);

            // Cập nhật thời gian đăng nhập cuối
            user.LastLogin = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
    }
} 