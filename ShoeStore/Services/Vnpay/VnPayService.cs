using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ShoeStore.Helpers;
using ShoeStore.Models.Payment;

namespace ShoeStore.Services.Payment
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> CreatePaymentUrl(VNPayInformationModel model, HttpContext context)
        {
            try
            {
                Console.WriteLine("=== Starting CreatePaymentUrl ===");
                
                // Kiểm tra cấu hình
                var tmnCode = _configuration["VnPay:TmnCode"];
                var hashSecret = _configuration["VnPay:HashSecret"];
                var baseUrl = _configuration["VnPay:BaseUrl"];
                
                Console.WriteLine($"Config - TmnCode: {tmnCode}");
                Console.WriteLine($"Config - BaseUrl: {baseUrl}");

                if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret) || string.IsNullOrEmpty(baseUrl))
                {
                    throw new Exception("Thiếu thông tin cấu hình VNPay");
                }

                var pay = new VnPayLibrary();
                var currentTime = DateTime.Now;

                // Thêm thông tin thanh toán
                pay.AddRequestData("vnp_Version", _configuration["VnPay:Version"] ?? "2.1.0");
                pay.AddRequestData("vnp_Command", "pay");
                pay.AddRequestData("vnp_TmnCode", tmnCode);
                pay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());
                pay.AddRequestData("vnp_BankCode", ""); // Để trống để hiện tất cả ngân hàng
                pay.AddRequestData("vnp_CreateDate", currentTime.ToString("yyyyMMddHHmmss"));
                pay.AddRequestData("vnp_CurrCode", "VND");
                pay.AddRequestData("vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString() ?? "::1");
                pay.AddRequestData("vnp_Locale", "vn");
                pay.AddRequestData("vnp_OrderInfo", model.OrderDescription);
                pay.AddRequestData("vnp_OrderType", "other");
                pay.AddRequestData("vnp_ReturnUrl", _configuration["VnPay:PaymentBackReturnUrl"]);
                pay.AddRequestData("vnp_TxnRef", model.OrderId);

                // Thông tin tùy chọn
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                    pay.AddRequestData("vnp_Bill_Mobile", model.PhoneNumber);
                if (!string.IsNullOrEmpty(model.Email))
                    pay.AddRequestData("vnp_Bill_Email", model.Email);
                if (!string.IsNullOrEmpty(model.Name))
                    pay.AddRequestData("vnp_Bill_FirstName", model.Name);
                if (!string.IsNullOrEmpty(model.Address))
                    pay.AddRequestData("vnp_Bill_Address", model.Address);
                
                pay.AddRequestData("vnp_Bill_City", "Ho Chi Minh");
                pay.AddRequestData("vnp_Bill_Country", "VN");

                Console.WriteLine("=== Request Data ===");
                Console.WriteLine($"Amount: {model.Amount * 100}");
                Console.WriteLine($"OrderId: {model.OrderId}");
                Console.WriteLine($"OrderInfo: {model.OrderDescription}");

                var paymentUrl = pay.CreateRequestUrl(baseUrl, hashSecret);

                Console.WriteLine($"Generated URL: {paymentUrl}");

                return paymentUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreatePaymentUrl: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public VNPayResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_SecureHash = collections.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value;
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _configuration["VnPay:HashSecret"]);

            if (!checkSignature)
            {
                return new VNPayResponseModel
                {
                    Success = false
                };
            }

            return new VNPayResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderDescription = vnp_OrderInfo,
                OrderId = vnp_orderId,
                TransactionId = vnp_TransactionId,
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode
            };
        }
    }
} 