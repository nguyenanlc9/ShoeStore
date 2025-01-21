using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models.Payment.Momo
{
    public class MomoTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; }  // Mã đơn hàng của hệ thống

        [Required] 
        [StringLength(50)]
        public string TransactionId { get; set; }  // Mã giao dịch từ MOMO

        [Required]
        [StringLength(50)]
        public string RequestId { get; set; }  // Request ID gửi lên MOMO

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }  // Số tiền giao dịch

        [Required]
        public DateTime TransactionDate { get; set; }  // Thời gian giao dịch

        [Required]
        [StringLength(50)]
        public string PayType { get; set; }  // Phương thức thanh toán (ví/thẻ)

        [StringLength(255)]
        public string ResponseMessage { get; set; }  // Message từ MOMO

        public int ResultCode { get; set; }  // Mã kết quả từ MOMO

        [StringLength(255)] 
        public string ExtraData { get; set; }  // Dữ liệu thêm nếu có

        public int OrderId { get; set; }  // Foreign key tới Order
        
        [ForeignKey("OrderId")]
        public Order Order { get; set; }
    }
} 