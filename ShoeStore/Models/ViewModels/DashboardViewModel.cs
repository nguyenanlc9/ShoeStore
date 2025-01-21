using ShoeStore.Models;
using ShoeStore.Models.Enums;
using System.Collections.Generic;

namespace ShoeStore.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; } // Đơn hàng trong ngày
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<TopSellingProduct> TopSellingProducts { get; set; }
        public string SelectedPeriod { get; set; }
        public List<DailyRevenueData> DailyRevenue { get; set; }
        public List<DailyRevenue> DailyRevenueList { get; set; }
        public List<PaymentStats> PaymentStats { get; set; }

        // Thống kê đơn hàng theo trạng thái
        public Dictionary<OrderStatus, int> OrderStatusStats { get; set; }

        // Thống kê đơn hàng theo phương thức thanh toán
        public Dictionary<string, int> PaymentMethodStats { get; set; }

        // Thống kê đánh giá sản phẩm
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }

        // Thống kê người dùng mới
        public int NewUsers { get; set; }

        // Top khách hàng chi tiêu cao nhất
        public List<TopSpendingCustomer> TopSpendingCustomers { get; set; }
    }

    public class TopSellingProduct
    {
        public Product Product { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class DailyRevenueData
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopSpendingCustomer
    {
        public User User { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentStats
    {
        public PaymentMethod Method { get; set; }
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }
} 