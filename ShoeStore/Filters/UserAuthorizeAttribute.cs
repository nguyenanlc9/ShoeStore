using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.Filters
{
    public class UserAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userInfo = context.HttpContext.Session.Get<User>("userInfo");
            if (userInfo == null || userInfo.RoleID != 1) // Kiểm tra RoleID = 1 (User)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            }
            base.OnActionExecuting(context);
        }
    }
} 