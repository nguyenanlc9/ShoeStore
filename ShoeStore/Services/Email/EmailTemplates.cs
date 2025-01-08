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
        var orderDetails = string.Join("\n",
            order.OrderDetails.Select(item => $@"
                <tr>
                    <td style='padding: 12px; border-bottom: 1px solid #eee;'>
                        <span style='color: #333;'>{item.Product.Name}</span>
                    </td>
                    <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: center;'>
                        {item.Size.SizeValue}
                    </td>
                    <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: center;'>
                        {item.Quantity}
                    </td>
                    <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: right;'>
                        {item.Price.ToString("N0")} ₫
                    </td>
                    <td style='padding: 12px; border-bottom: 1px solid #eee; text-align: right;'>
                        {(item.Price * item.Quantity).ToString("N0")} ₫
                    </td>
                </tr>"));

        var subtotal = order.OrderDetails.Sum(x => x.Price * x.Quantity);
        var discountInfo = "";
        
        // Thêm thông tin giảm giá thành viên nếu có
        if (order.User?.MemberRank != null)
        {
            var memberDiscount = subtotal * (order.User.MemberRank.DiscountPercent / 100m);
            discountInfo += $@"
                <tr>
                    <td colspan='4' style='padding: 12px; text-align: right; color: #28a745;'>
                        Giảm giá thành viên ({order.User.MemberRank.RankName} - {order.User.MemberRank.DiscountPercent}%):
                    </td>
                    <td style='padding: 12px; text-align: right; color: #28a745;'>
                        -{memberDiscount.ToString("N0")} ₫
                    </td>
                </tr>";
        }

        // Thêm thông tin mã giảm giá nếu có
        if (!string.IsNullOrEmpty(order.OrderCoupon))
        {
            var couponDiscount = subtotal * ((order.Coupon?.DiscountPercentage ?? 0) / 100m);
            discountInfo += $@"
                <tr>
                    <td colspan='4' style='padding: 12px; text-align: right; color: #28a745;'>
                        Mã giảm giá ({order.OrderCoupon} - {(order.Coupon?.DiscountPercentage ?? 0)}%):
                    </td>
                    <td style='padding: 12px; text-align: right; color: #28a745;'>
                        -{couponDiscount.ToString("N0")} ₫
                    </td>
                </tr>";
        }

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Xác nhận đơn hàng</title>
            </head>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px;'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h1 style='color: #28a745;'>ShoeStore</h1>
                </div>

                <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin-bottom: 30px;'>
                    <h2 style='color: #28a745; margin-bottom: 20px;'>Cảm ơn bạn đã đặt hàng!</h2>
                    <p>Xin chào <strong>{order.OrderUsName}</strong>,</p>
                    <p>Đơn hàng của bạn đã được xác nhận và đang được xử lý.</p>
                </div>

                <div style='margin-bottom: 30px;'>
                    <h3 style='color: #333; border-bottom: 2px solid #28a745; padding-bottom: 10px;'>Thông tin đơn hàng</h3>
                    <p><strong>Mã đơn hàng:</strong> {order.OrderCode}</p>
                    <p><strong>Ngày đặt:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Phương thức thanh toán:</strong> {order.PaymentMethod}</p>
                    <p><strong>Trạng thái thanh toán:</strong> 
                        <span style='color: {(order.PaymentStatus == PaymentStatus.Completed ? "#28a745" : "#ffc107")};'>
                            {order.PaymentStatus}
                        </span>
                    </p>
                </div>

                <div style='margin-bottom: 30px;'>
                    <h3 style='color: #333; border-bottom: 2px solid #28a745; padding-bottom: 10px;'>Chi tiết đơn hàng</h3>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr style='background-color: #f8f9fa;'>
                                <th style='padding: 12px; text-align: left;'>Sản phẩm</th>
                                <th style='padding: 12px; text-align: center;'>Size</th>
                                <th style='padding: 12px; text-align: center;'>Số lượng</th>
                                <th style='padding: 12px; text-align: right;'>Đơn giá</th>
                                <th style='padding: 12px; text-align: right;'>Thành tiền</th>
                            </tr>
                        </thead>
                        <tbody>
                            {orderDetails}
                            <tr>
                                <td colspan='4' style='padding: 12px; text-align: right;'>
                                    <strong>Tạm tính:</strong>
                                </td>
                                <td style='padding: 12px; text-align: right;'>
                                    {subtotal.ToString("N0")} ₫
                                </td>
                            </tr>
                            {discountInfo}
                            <tr>
                                <td colspan='4' style='padding: 12px; text-align: right; font-size: 18px;'>
                                    <strong>Tổng tiền:</strong>
                                </td>
                                <td style='padding: 12px; text-align: right; color: #28a745; font-size: 18px; font-weight: bold;'>
                                    {order.TotalAmount.ToString("N0")} ₫
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <div style='margin-bottom: 30px;'>
                    <h3 style='color: #333; border-bottom: 2px solid #28a745; padding-bottom: 10px;'>Thông tin giao hàng</h3>
                    <p><strong>Người nhận:</strong> {order.OrderUsName}</p>
                    <p><strong>Số điện thoại:</strong> {order.PhoneNumber}</p>
                    <p><strong>Địa chỉ:</strong> {order.ShippingAddress}</p>
                    {(!string.IsNullOrEmpty(order.Notes) ? $"<p><strong>Ghi chú:</strong> {order.Notes}</p>" : "")}
                </div>

                <div style='background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin-bottom: 30px;'>
                    <p style='margin-bottom: 10px;'>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua:</p>
                    <p><strong>Email:</strong> shoestorebe@gmail.com</p>
                    <p><strong>Hotline:</strong> 1900 xxxx</p>
                </div>

                <div style='text-align: center; color: #666; font-size: 14px;'>
                    <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                    <p>&copy; 2024 ShoeStore. All rights reserved.</p>
                </div>
            </body>
            </html>";
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