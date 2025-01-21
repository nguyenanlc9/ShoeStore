using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Data;
using ShoeStore.Hubs;
using ShoeStore.Models;
using System;
using System.Threading.Tasks;

namespace ShoeStore.Services
{
    public interface INotificationService
    {
        Task SendOrderNotification(string message, string orderId, string type);
        Task SendGeneralNotification(string message, string type);
        Task<List<Notification>> GetUnreadNotifications();
        Task MarkAsRead(int notificationId);
        Task MarkAllAsRead();
    }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;

        public NotificationService(IHubContext<NotificationHub> hubContext, ApplicationDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        public async Task SendOrderNotification(string message, string orderId, string type)
        {
            // Lưu thông báo vào database
            var notification = new Notification
            {
                Message = message,
                Type = type,
                ReferenceId = orderId,
                CreatedAt = DateTime.Now,
                IsRead = false,
                Url = $"/Admin/Order/Details/{orderId}"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi thông báo realtime
            await _hubContext.Clients.All.SendAsync("ReceiveOrderNotification", message, orderId, type);
        }

        public async Task SendGeneralNotification(string message, string type)
        {
            // Lưu thông báo vào database
            var notification = new Notification
            {
                Message = message,
                Type = type,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi thông báo realtime
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, type);
        }

        public async Task<List<Notification>> GetUnreadNotifications()
        {
            return await _context.Notifications
                .Where(n => !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsRead()
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
} 