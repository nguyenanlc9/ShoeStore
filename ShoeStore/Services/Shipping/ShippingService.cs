using ShoeStore.Models;

namespace ShoeStore.Services.Shipping
{
    public class ShippingService : IShippingService
    {
        private readonly Dictionary<string, decimal> _baseRates = new()
        {
            { "Hồ Chí Minh", 15000 },
            { "Hà Nội", 30000 },
            { "Đà Nẵng", 25000 },
            { "Default", 35000 }
        };

        private readonly Dictionary<string, int> _deliveryDays = new()
        {
            { "Hồ Chí Minh", 1 },
            { "Hà Nội", 3 },
            { "Đà Nẵng", 2 },
            { "Default", 4 }
        };

        public async Task<ShippingFeeResponse> CalculateShippingFee(ShippingFeeRequest request)
        {
            // Giả lập delay của API
            await Task.Delay(500);

            var response = new ShippingFeeResponse
            {
                Success = true,
                Message = "Tính phí vận chuyển thành công"
            };

            try
            {
                // Lấy phí cơ bản dựa trên tỉnh/thành
                decimal baseFee = _baseRates.GetValueOrDefault(request.Province, _baseRates["Default"]);
                
                // Tính phí theo cân nặng (mỗi kg tăng thêm 5000đ)
                decimal weightFee = Math.Max(0, request.Weight - 1) * 5000;
                
                // Giảm phí vận chuyển cho đơn hàng có giá trị cao
                decimal discount = 0;
                if (request.OrderAmount >= 1000000) // Trên 1 triệu
                {
                    discount = baseFee * 0.2m; // Giảm 20%
                }
                else if (request.OrderAmount >= 500000) // Trên 500k
                {
                    discount = baseFee * 0.1m; // Giảm 10%
                }
                
                // Tính tổng phí vận chuyển
                response.ShippingFee = Math.Max(0, baseFee + weightFee - discount);
                
                // Ước tính thời gian giao hàng
                int deliveryDays = _deliveryDays.GetValueOrDefault(request.Province, _deliveryDays["Default"]);
                response.EstimatedDeliveryTime = $"{deliveryDays}-{deliveryDays + 1} ngày";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Lỗi khi tính phí vận chuyển: " + ex.Message;
            }

            return response;
        }
    }
} 