using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.Filters
{
    public class AuthenticationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userInfo = context.HttpContext.Session.Get<User>("userInfo");
            
            if (userInfo == null)
            {
                // Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }
        }
    }
} 