using System;

namespace ShoeStore.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }  // new, cancel, return
        public string ReferenceId { get; set; }  // OrderId hoặc ReturnRequestId
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string Url { get; set; }  // Link đến trang chi tiết
    }
} 