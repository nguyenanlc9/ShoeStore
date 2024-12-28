using ShoeStore.Models.Payment;
using Microsoft.AspNetCore.Http;

namespace ShoeStore.Services.Payment
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model);
        MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
    }
} 