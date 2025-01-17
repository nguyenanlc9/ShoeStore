namespace ShoeStore.Models.Enums
{
    public enum OrderStatus
    {
        Pending = 0,        // Chờ xử lý
        Processing = 1,     // Đang xử lý
        Shipped = 2,        // Đã giao cho đơn vị vận chuyển
        Shipping = 3,       // Đang vận chuyển
        Completed = 4,      // Hoàn thành
        Cancelled = 5       // Đã hủy
    }
} 