using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using ShoesStore.Models;
using ShoesStore.Utils;

public class AdminAuthenticationAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userInfo = context.HttpContext.Session.Get<AdminUser>("userInfo");
        if (userInfo == null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "Admin" });
            return;
        }
        base.OnActionExecuting(context);
    }
}
