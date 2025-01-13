using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    public enum PaymentMethodType
    {
        COD = 1,
        VNPay = 2,
        Momo = 3,
        ZaloPay = 4,
        Visa = 5
    }

    public enum PaymentMethodStatus
    {
        Active = 1,
        Hidden = 2,
        Maintenance = 3
    }

    public class PaymentMethodConfig
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn loại phương thức thanh toán")]
        public PaymentMethodType Type { get; set; }
        
        [Required(ErrorMessage = "Tên phương thức thanh toán không được để trống")]
        [StringLength(50)]
        public string Name { get; set; }
        
        [Required]
        public PaymentMethodStatus Status { get; set; }
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        [StringLength(200)]
        public string? MaintenanceMessage { get; set; }
        
        public DateTime? LastUpdated { get; set; }
    }
} 