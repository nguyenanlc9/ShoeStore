using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    public class ShippingFeeRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
        public string Province { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn quận/huyện")]
        public string District { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phường/xã")]
        public string Ward { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng không hợp lệ")]
        public decimal OrderAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cân nặng không hợp lệ")]
        public decimal Weight { get; set; } = 1; // Mặc định 1kg
    }

    public class ShippingFeeResponse
    {
        public decimal ShippingFee { get; set; }
        public string EstimatedDeliveryTime { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class AddressModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
} 