using ShoeStore.Models.Payment;

namespace ShoeStore.Services.ZaloPay
{
    public interface IZaloPayService
    {
        Task<ZaloPayCreateOrderResponse> CreatePaymentAsync(OrderInfoModel model);
        Task<bool> VerifyCallbackAsync(IQueryCollection collection);
        Task<ZaloPayQueryResponse> QueryTransactionAsync(string appTransId);
        string GetMacCreateOrder(Dictionary<string, string> data);
        string GetMacCallback(Dictionary<string, string> data);
    }
} 