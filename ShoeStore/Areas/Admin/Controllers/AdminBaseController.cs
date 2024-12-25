using Microsoft.AspNetCore.Mvc;
using ShoeStore.Filters;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public abstract class AdminBaseController : Controller
    {
    }
} 