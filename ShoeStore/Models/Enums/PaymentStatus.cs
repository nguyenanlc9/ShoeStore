namespace ShoeStore.Models.Enums
{
    public enum PaymentStatus
    {
        Pending = 0,        // Đang xác thực
        Completed = 1,      // Thanh toán thành công
        Failed = 2,         // Thanh toán thất bại
        Refunded = 3,       // Đã hoàn tiền
        Cancelled = 4       // Đã hủy
    }
} 