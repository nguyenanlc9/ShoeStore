using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Filters;
using ClosedXML.Excel;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class VNPayTransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VNPayTransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, DateTime? fromDate, DateTime? toDate, string status)
        {
            ViewBag.CurrentSearch = searchString;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentStatus = status;

            var transactions = _context.VNPayTransactions
                .Include(t => t.Order)
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                transactions = transactions.Where(t => 
                    t.OrderId.Contains(searchString) || 
                    t.TransactionId.Contains(searchString) ||
                    t.BankTranNo.Contains(searchString));
            }

            // Lọc theo ngày
            if (fromDate.HasValue)
            {
                transactions = transactions.Where(t => t.PaymentTime >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                transactions = transactions.Where(t => t.PaymentTime <= toDate.Value.AddDays(1));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                transactions = transactions.Where(t => t.TransactionStatus == status);
            }

            // Sắp xếp theo thời gian mới nhất
            transactions = transactions.OrderByDescending(t => t.PaymentTime);

            return View(await transactions.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.VNPayTransactions
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        public async Task<IActionResult> ExportToExcel(string searchString, DateTime? fromDate, DateTime? toDate, string status)
        {
            var transactions = _context.VNPayTransactions
                .Include(t => t.Order)
                .AsQueryable();

            // Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(searchString))
            {
                transactions = transactions.Where(t => 
                    t.OrderId.Contains(searchString) || 
                    t.TransactionId.Contains(searchString) ||
                    t.BankTranNo.Contains(searchString));
            }

            if (fromDate.HasValue)
            {
                transactions = transactions.Where(t => t.PaymentTime >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                transactions = transactions.Where(t => t.PaymentTime <= toDate.Value.AddDays(1));
            }

            if (!string.IsNullOrEmpty(status))
            {
                transactions = transactions.Where(t => t.TransactionStatus == status);
            }

            var data = await transactions.OrderByDescending(t => t.PaymentTime).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("VNPay Transactions");

                // Thêm tiêu đề
                worksheet.Cell(1, 1).Value = "Mã đơn hàng";
                worksheet.Cell(1, 2).Value = "Mã giao dịch";
                worksheet.Cell(1, 3).Value = "Số tiền";
                worksheet.Cell(1, 4).Value = "Thời gian";
                worksheet.Cell(1, 5).Value = "Ngân hàng";
                worksheet.Cell(1, 6).Value = "Mã GD ngân hàng";
                worksheet.Cell(1, 7).Value = "Loại thẻ";
                worksheet.Cell(1, 8).Value = "Trạng thái";

                // Thêm dữ liệu
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = item.OrderId;
                    worksheet.Cell(row, 2).Value = item.TransactionId;
                    worksheet.Cell(row, 3).Value = item.Amount;
                    worksheet.Cell(row, 4).Value = item.PaymentTime;
                    worksheet.Cell(row, 5).Value = item.BankCode;
                    worksheet.Cell(row, 6).Value = item.BankTranNo;
                    worksheet.Cell(row, 7).Value = item.CardType;
                    worksheet.Cell(row, 8).Value = GetTransactionStatus(item.ResponseCode);
                    row++;
                }

                // Format cột thời gian
                worksheet.Column(4).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                
                // Format cột số tiền
                worksheet.Column(3).Style.NumberFormat.Format = "#,##0";

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Tạo file Excel
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"VNPay_Transactions_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private string GetTransactionStatus(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Thành công",
                "24" => "Khách hàng hủy GD",
                "97" => "Chữ ký không hợp lệ",
                _ => "Lỗi"
            };
        }
    }
} 