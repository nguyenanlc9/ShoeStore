using Microsoft.AspNetCore.Mvc;
using ShoeStore.Services;
using System.Threading.Tasks;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            var notifications = await _notificationService.GetUnreadNotifications();
            return Json(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsRead(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notificationService.MarkAllAsRead();
            return Json(new { success = true });
        }
    }
} 