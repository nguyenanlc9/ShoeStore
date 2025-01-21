using ClosedXML.Excel;
using ShoeStore.Models;
using ShoeStore.Models.Enums;

namespace ShoeStore.Services.Excel
{
    public interface IExcelService
    {
        byte[] ExportOrders(List<Models.Order> orders);
        byte[] ExportRevenueReport(DateTime fromDate, DateTime toDate, List<RevenueReportData> reportData);
    }

    public class ExcelService : IExcelService
    {
        public byte[] ExportOrders(List<Models.Order> orders)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Danh sách đơn hàng");

                // Thiết lập header
                worksheet.Cell(1, 1).Value = "Mã đơn hàng";
                worksheet.Cell(1, 2).Value = "Ngày đặt";
                worksheet.Cell(1, 3).Value = "Khách hàng";
                worksheet.Cell(1, 4).Value = "Số điện thoại";
                worksheet.Cell(1, 5).Value = "Địa chỉ";
                worksheet.Cell(1, 6).Value = "Phương thức thanh toán";
                worksheet.Cell(1, 7).Value = "Tổng tiền";
                worksheet.Cell(1, 8).Value = "Trạng thái";

                // Style cho header
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Thêm dữ liệu
                int row = 2;
                foreach (var order in orders)
                {
                    worksheet.Cell(row, 1).Value = order.OrderCode;
                    worksheet.Cell(row, 2).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 3).Value = order.OrderUsName;
                    worksheet.Cell(row, 4).Value = order.PhoneNumber;
                    worksheet.Cell(row, 5).Value = order.ShippingAddress;
                    worksheet.Cell(row, 6).Value = GetPaymentMethodText(order.PaymentMethod);
                    worksheet.Cell(row, 7).Value = order.TotalAmount;
                    worksheet.Cell(row, 8).Value = GetOrderStatusText(order.Status);

                    // Style cho cột số tiền
                    worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                    
                    // Style cho trạng thái
                    var statusCell = worksheet.Cell(row, 8);
                    switch (order.Status)
                    {
                        case OrderStatus.Completed:
                            statusCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            break;
                        case OrderStatus.Cancelled:
                            statusCell.Style.Fill.BackgroundColor = XLColor.LightPink;
                            break;
                    }

                    row++;
                }

                // Tự động điều chỉnh độ rộng cột
                worksheet.Columns().AdjustToContents();

                // Thêm border
                var range = worksheet.Range(1, 1, row - 1, 8);
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public byte[] ExportRevenueReport(DateTime fromDate, DateTime toDate, List<RevenueReportData> reportData)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Báo cáo doanh thu");

                // Header chung
                worksheet.Cell(1, 1).Value = "BÁO CÁO DOANH THU";
                worksheet.Range(1, 1, 1, 6).Merge();
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Thông tin thời gian
                worksheet.Cell(2, 1).Value = $"Từ ngày: {fromDate:dd/MM/yyyy} - Đến ngày: {toDate:dd/MM/yyyy}";
                worksheet.Range(2, 1, 2, 6).Merge();
                worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Headers
                int headerRow = 4;
                worksheet.Cell(headerRow, 1).Value = "Ngày";
                worksheet.Cell(headerRow, 2).Value = "Số đơn hàng";
                worksheet.Cell(headerRow, 3).Value = "Doanh thu";
                worksheet.Cell(headerRow, 4).Value = "Đơn hủy";
                worksheet.Cell(headerRow, 5).Value = "Tỷ lệ hoàn thành";
                worksheet.Cell(headerRow, 6).Value = "Ghi chú";

                // Style cho header
                var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Thêm dữ liệu
                int row = headerRow + 1;
                foreach (var data in reportData)
                {
                    worksheet.Cell(row, 1).Value = data.Date.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 2).Value = data.TotalOrders;
                    worksheet.Cell(row, 3).Value = data.Revenue;
                    worksheet.Cell(row, 4).Value = data.CancelledOrders;
                    worksheet.Cell(row, 5).Value = data.CompletionRate;
                    worksheet.Cell(row, 6).Value = data.Note;

                    // Format số tiền
                    worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                    // Format tỷ lệ
                    worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.00%";

                    row++;
                }

                // Thêm tổng cộng
                worksheet.Cell(row, 1).Value = "Tổng cộng";
                worksheet.Cell(row, 2).FormulaA1 = $"=SUM(B{headerRow + 1}:B{row - 1})";
                worksheet.Cell(row, 3).FormulaA1 = $"=SUM(C{headerRow + 1}:C{row - 1})";
                worksheet.Cell(row, 4).FormulaA1 = $"=SUM(D{headerRow + 1}:D{row - 1})";
                worksheet.Cell(row, 5).FormulaA1 = $"=AVERAGE(E{headerRow + 1}:E{row - 1})";

                // Style cho dòng tổng
                var totalRow = worksheet.Range(row, 1, row, 6);
                totalRow.Style.Font.Bold = true;
                totalRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Tự động điều chỉnh độ rộng cột
                worksheet.Columns().AdjustToContents();

                // Thêm border
                var range = worksheet.Range(headerRow, 1, row, 6);
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private string GetPaymentMethodText(PaymentMethod paymentMethod)
        {
            return paymentMethod switch
            {
                PaymentMethod.COD => "Thanh toán khi nhận hàng",
                PaymentMethod.VNPay => "VNPay",
                PaymentMethod.Momo => "Ví MoMo",
                PaymentMethod.ZaloPay => "ZaloPay",
                _ => paymentMethod.ToString()
            };
        }

        private string GetOrderStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Processing => "Đang xử lý",
                OrderStatus.Shipping => "Đang giao hàng",
                OrderStatus.Completed => "Hoàn thành",
                OrderStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }
    }

    public class RevenueReportData
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public decimal Revenue { get; set; }
        public int CancelledOrders { get; set; }
        public decimal CompletionRate { get; set; }
        public string Note { get; set; }
    }
} 