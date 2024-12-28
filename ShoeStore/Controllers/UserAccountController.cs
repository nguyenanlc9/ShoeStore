using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Utils;
using System.Net.Mail;
using System.Net;

namespace ShoeStore.Controllers
{
    public class UserAccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string UserSessionKey = "userInfo";

        public UserAccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            var userInfo = HttpContext.Session.Get<User>(UserSessionKey);
            if (userInfo != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginDTO model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.UserName);

                if (user != null && PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
                {
                    HttpContext.Session.Set(UserSessionKey, user);
                    return RedirectToAction("Index", "Home");
                }

                ViewData["Message"] = "Tài khoản hoặc mật khẩu không đúng!";
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserRegisterDTO model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ViewData["Message"] = "Tên đăng nhập đã được sử dụng!";
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ViewData["Message"] = "Email đã được sử dụng!";
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    FullName = model.FullName,
                    Phone = model.Phone,
                    RoleID = 2,
                    RegisterDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    Status = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                HttpContext.Session.Set("userInfo", user);
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ViewData["Message"] = "Email không tồn tại trong hệ thống!";
                    return View(model);
                }

                // Tạo mật khẩu mới ngẫu nhiên
                var newPassword = GenerateRandomPassword();
                user.PasswordHash = PasswordHelper.HashPassword(newPassword);

                // Gửi email mật khẩu mới
                try
                {
                    await SendPasswordResetEmail(user.Email, newPassword);
                    await _context.SaveChangesAsync();

                    ViewData["MessageType"] = "success";
                    ViewData["Message"] = "Mật khẩu mới đã được gửi đến email của bạn!";
                }
                catch
                {
                    ViewData["Message"] = "Có lỗi xảy ra khi gửi email!";
                }
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove(UserSessionKey);
            return RedirectToAction("Login");
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task SendPasswordResetEmail(string email, string newPassword)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("your-email@gmail.com"),
                Subject = "Đặt lại mật khẩu - ShoeStore",
                Body = $"Mật khẩu mới của bạn là: {newPassword}",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}