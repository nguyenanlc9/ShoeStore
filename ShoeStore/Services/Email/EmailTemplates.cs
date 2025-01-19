using ShoeStore.Models;
using ShoeStore.Models.Enums;
using System.IO;
using System.Text;

public static class EmailTemplates
{
    private static string ConvertImageToBase64(string imagePath)
    {
        try
        {
            if (string.IsNullOrEmpty(imagePath)) return "";
            
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
            if (!File.Exists(fullPath)) return "";

            byte[] imageBytes = File.ReadAllBytes(fullPath);
            string base64String = Convert.ToBase64String(imageBytes);
            string extension = Path.GetExtension(imagePath).TrimStart('.').ToLower();
            
            return $"data:image/{extension};base64,{base64String}";
        }
        catch
        {
            return "";
        }
    }

    public static string GetOrderConfirmationEmail(Order order)
    {
        var template = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2>Xác nhận đơn hàng #{order.OrderCode}</h2>
                <p>Cảm ơn bạn đã đặt hàng tại ShoeStore!</p>

                <div style='margin: 20px 0; padding: 15px; border: 1px solid #ddd;'>
                    <h3>Thông tin đơn hàng</h3>
                    <p><strong>Mã đơn hàng:</strong> {order.OrderCode}</p>
                    <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Trạng thái:</strong> {GetOrderStatus(order.Status)}</p>
                    <p><strong>Phương thức thanh toán:</strong> {GetPaymentMethod(order.PaymentMethod)}</p>
                    <p><strong>Trạng thái thanh toán:</strong> {GetPaymentStatus(order.PaymentStatus)}</p>
                </div>

                <div style='margin: 20px 0;'>
                    <h3>Chi tiết đơn hàng</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr style='background-color: #f8f9fa;'>
                            <th style='padding: 10px; text-align: left; border: 1px solid #ddd;'>Sản phẩm</th>
                            <th style='padding: 10px; text-align: right; border: 1px solid #ddd;'>Giá</th>
                        </tr>";

        foreach (var item in order.OrderDetails)
        {
            template += $@"
                        <tr>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{item.Product.Name} x {item.Quantity} ({item.Size.SizeValue})</td>
                            <td style='padding: 10px; text-align: right; border: 1px solid #ddd;'>{item.Price:N0} ₫</td>
                        </tr>";
        }

        template += $@"
                    </table>
                </div>

                <div style='margin: 20px 0;'>
                    <h3>Tổng thanh toán</h3>
                    <table style='width: 100%;'>
                        <tr>
                            <td style='padding: 5px 0;'>Tạm tính:</td>
                            <td style='text-align: right;'>{order.SubTotal:N0} ₫</td>
                        </tr>";

        if (order.MemberDiscount > 0)
        {
            template += $@"
                        <tr>
                            <td style='padding: 5px 0;'>Giảm giá thành viên:</td>
                            <td style='text-align: right; color: #28a745;'>-{order.MemberDiscount:N0} ₫</td>
                        </tr>";
        }

        if (order.CouponDiscount > 0)
        {
            template += $@"
                        <tr>
                            <td style='padding: 5px 0;'>Giảm giá từ mã:</td>
                            <td style='text-align: right; color: #28a745;'>-{order.CouponDiscount:N0} ₫</td>
                        </tr>";
        }

        template += $@"
                        <tr>
                            <td style='padding: 5px 0;'>Phí vận chuyển:</td>
                            <td style='text-align: right;'>{order.ShipFeeGHN:N0} ₫</td>
                        </tr>
                        <tr style='font-weight: bold;'>
                            <td style='padding: 5px 0;'>Tổng cộng:</td>
                            <td style='text-align: right; color: #007bff;'>{order.TotalAmount:N0} ₫</td>
                        </tr>
                    </table>
                </div>

                <div style='margin: 20px 0; padding: 15px; border: 1px solid #ddd;'>
                    <h3>Thông tin giao hàng</h3>
                    <p><strong>Người nhận:</strong> {order.OrderUsName}</p>
                    <p><strong>Số điện thoại:</strong> {order.PhoneNumber}</p>
                    <p><strong>Địa chỉ:</strong> {order.ShippingAddress}</p>
                    {(!string.IsNullOrEmpty(order.Notes) ? $"<p><strong>Ghi chú:</strong> {order.Notes}</p>" : "")}
                </div>

                <p style='margin-top: 20px;'>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua email hoặc hotline.</p>
                <p>Trân trọng,<br>ShoeStore Team</p>
            </div>";

        return template;
    }

    private static string GetOrderStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Chờ xử lý",
            OrderStatus.Processing => "Đang xử lý",
            OrderStatus.Shipping => "Đang giao hàng",
            OrderStatus.Completed => "Đã hoàn thành",
            OrderStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
    }

    private static string GetPaymentMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.COD => "Thanh toán khi nhận hàng (COD)",
            PaymentMethod.VNPay => "VNPay",
            PaymentMethod.Momo => "Momo",
            PaymentMethod.ZaloPay => "ZaloPay",
            _ => "Không xác định"
        };
    }

    private static string GetPaymentStatus(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Chưa thanh toán",
            PaymentStatus.Completed => "Đã thanh toán",
            PaymentStatus.Failed => "Thanh toán thất bại",
            PaymentStatus.Refunded => "Đã hoàn tiền",
            _ => "Không xác định"
        };
    }

    public static string GetResetPasswordEmail(string fullName, string newPassword)
    {
        return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #333;'>Đặt lại mật khẩu</h2>
                
                <p>Xin chào {fullName},</p>
                
                <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu của bạn.</p>
                
                <div style='background-color: #f8f9fa; padding: 15px; margin: 15px 0;'>
                    <p>Mật khẩu mới của bạn là: <strong>{newPassword}</strong></p>
                </div>

                <p>Vui lòng đăng nhập và đổi mật khẩu mới ngay sau khi nhận được email này.</p>
                
                <p style='color: #666;'>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng liên hệ với chúng tôi ngay.</p>
                
                <p style='color: #666;'>Trân trọng,<br/>ShoeStore Team</p>
            </div>";
    }
} 