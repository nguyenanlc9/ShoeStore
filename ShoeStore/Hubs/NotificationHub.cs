using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ShoeStore.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(string message, string type)
        {
            _logger.LogInformation($"Sending notification: {message}, Type: {type}");
            await Clients.All.SendAsync("ReceiveNotification", message, type);
        }

        public async Task SendOrderNotification(string message, string orderId, string type)
        {
            _logger.LogInformation($"Sending order notification: {message}, OrderId: {orderId}, Type: {type}");
            await Clients.All.SendAsync("ReceiveOrderNotification", message, orderId, type);
        }
    }
} 