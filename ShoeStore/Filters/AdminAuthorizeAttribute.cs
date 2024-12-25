using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        private const string AdminSessionKey = "AdminUserInfo";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var adminInfo = context.HttpContext.Session.Get<User>(AdminSessionKey);
            if (adminInfo == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "Admin" });
                return;
            }
            
            if (adminInfo.RoleID != 2) 
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "Admin" });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
} 