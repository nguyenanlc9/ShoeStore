using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Enums;

namespace ShoeStore.Services.Order
{
    public class OrderProcessingService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceProvider _serviceProvider;

        public OrderProcessingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            CheckOrderStatus().Wait();
        }

        private async Task CheckOrderStatus()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var fifteenMinutesAgo = DateTime.Now.AddMinutes(-10);

                var pendingOrders = await context.Orders
                    .Where(o => o.Status == OrderStatus.Pending
                           && o.CreatedAt <= fifteenMinutesAgo
                           && o.PaymentStatus != PaymentStatus.Completed)
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    order.Status = OrderStatus.Cancelled;
                    order.PaymentStatus = PaymentStatus.Failed;
                    order.CancelReason = "Đơn hàng đã quá thời gian thanh toán (10 phút)";
                }

                if (pendingOrders.Any())
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Cancelled {pendingOrders.Count} orders due to payment timeout");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}