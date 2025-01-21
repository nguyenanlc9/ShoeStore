using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("VNPayTransactions")]
    public class VNPayTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OrderId { get; set; }  // Mã đơn hàng của bạn

        [Required]
        public string TransactionId { get; set; }  // Mã giao dịch VNPay

        public string PaymentMethod { get; set; }  // Phương thức thanh toán
        
        public decimal Amount { get; set; }  // Số tiền

        public DateTime PaymentTime { get; set; }  // Thời gian thanh toán

        public string BankCode { get; set; }  // Mã ngân hàng

        public string BankTranNo { get; set; }  // Mã giao dịch của ngân hàng

        public string CardType { get; set; }  // Loại thẻ

        public string ResponseCode { get; set; }  // Mã phản hồi

        public string TransactionStatus { get; set; }  // Trạng thái giao dịch

        public string SecureHash { get; set; }  // Mã hash bảo mật

        // Foreign key
        public int OrderRefId { get; set; }  // ID tham chiếu đến bảng Orders
        
        [ForeignKey("OrderRefId")]
        public virtual Order Order { get; set; }
    }
} 