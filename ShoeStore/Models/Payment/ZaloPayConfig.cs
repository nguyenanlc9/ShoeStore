using System;

namespace ShoeStore.Models.Payment
{
    public class ZaloPayConfig
    {
        public string AppId { get; set; }
        public string Key1 { get; set; }
        public string Key2 { get; set; }
        public string CreateOrderUrl { get; set; }
        public string QueryOrderUrl { get; set; }
        public string CallbackUrl { get; set; }
        public string RedirectUrl { get; set; }
    }
} 