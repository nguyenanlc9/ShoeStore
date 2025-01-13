using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.Payment
{
    public class OrderInfoModel
    {
        [Required]
        public string OrderId { get; set; }

        [Required]
        public long Amount { get; set; }

        [Required]
        public string OrderInfo { get; set; }

        public string FullName { get; set; }
    }
} 