using ShoeStore.Models;

namespace ShoeStore.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalCustomers { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<TopSellingProduct> TopSellingProducts { get; set; }
        public string SelectedPeriod { get; set; }
        public List<DailyRevenueData> DailyRevenue { get; set; }
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
} 