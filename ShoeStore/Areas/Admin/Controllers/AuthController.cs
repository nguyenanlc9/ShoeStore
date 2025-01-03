using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using Microsoft.AspNetCore.Http;
using ShoeStore.Utils;
using ShoeStore.Models.DTO.Request;
using ShoeStore.Filters;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string AdminSessionKey = "AdminUserInfo";
        private const string AdminCookieKey = "AdminCredential";
        private readonly string _encryptionKey = "YourSecretKey123";

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            var adminInfo = HttpContext.Session.Get<User>(AdminSessionKey);
            if (adminInfo?.RoleID == 2)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            try
            {
                var encryptedToken = Request.Cookies[AdminCookieKey];
                if (!string.IsNullOrEmpty(encryptedToken))
                {
                    var tokenData = DecryptString(encryptedToken, _encryptionKey);
                    var username = tokenData.Split('|')[0];

                    var user = _context.Users
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Username == username);

                    if (user != null && user.RoleID == 2)
                    {
                        HttpContext.Session.Set(AdminSessionKey, user);
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                    else
                    {
                        Response.Cookies.Delete(AdminCookieKey);
                    }
                }
            }
            catch
            {
                Response.Cookies.Delete(AdminCookieKey);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginDTO login)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == login.UserName);

            if (user != null && PasswordHelper.VerifyPassword(login.Password, user.PasswordHash))
            {
                if (user.RoleID == 2)
                {
                    HttpContext.Session.Set(AdminSessionKey, user);

                    if (login.RememberMe)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30),
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Lax,
                            IsEssential = true
                        };

                        var tokenData = $"{user.Username}|{DateTime.Now.Ticks}";
                        var encryptedToken = EncryptString(tokenData, _encryptionKey);

                        Response.Cookies.Append(AdminCookieKey, encryptedToken, cookieOptions);
                    }

                    return RedirectToAction("Index", "Home", new { area = "Admin" });
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
            HttpContext.Session.Remove(AdminSessionKey);
            Response.Cookies.Delete(AdminCookieKey);
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private string EncryptString(string text, string key)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(text);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        private string DecryptString(string cipherText, string key)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
