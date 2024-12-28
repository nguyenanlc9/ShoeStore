using Microsoft.AspNetCore.Http;
using ShoeStore.Models.Payment;

namespace ShoeStore.Services.Payment
{
    public interface IVnPayService
    {
        Task<string> CreatePaymentUrl(VNPayInformationModel model, HttpContext context);
        VNPayResponseModel PaymentExecute(IQueryCollection collections);
    }
} 