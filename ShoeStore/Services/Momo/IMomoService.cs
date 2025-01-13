using Microsoft.AspNetCore.Http;
using ShoeStore.Models.Payment;
using ShoeStore.Models.Payment.Momo;

namespace ShoeStore.Services.Momo
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponse> CreatePaymentAsync(OrderInfoModel model);
        Task<bool> PaymentExecuteAsync(Dictionary<string, string> queryParams);
    }
} 