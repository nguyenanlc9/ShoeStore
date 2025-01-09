using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.Enums
{
    public enum PaymentMethod
    {
        [Display(Name = "Thanh toán khi nhận hàng")]
        Cash = 0,

        [Display(Name = "Thanh toán qua VNPay")]
        VNPay = 1,

        [Display(Name = "Thanh toán qua Momo")]
        Momo = 2,

        [Display(Name = "Thanh toán qua ZaloPay")]
        ZaloPay = 3
    }
} 