namespace ShoeStore.Models
{
    public class RevenueStatistics
    {
        public string Period { get; set; } // Tháng/Năm
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }
}
