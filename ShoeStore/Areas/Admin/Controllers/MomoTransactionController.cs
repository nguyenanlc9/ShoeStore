using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.Payment.Momo;
using ShoeStore.Filters;
using System.Text.Json;
using ClosedXML.Excel;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class MomoTransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MomoTransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.MomoTransactions
                .Include(m => m.Order)
                .AsQueryable();

            // Áp dụng filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => 
                    m.OrderCode.Contains(searchString) || 
                    m.TransactionId.Contains(searchString) ||
                    m.RequestId.Contains(searchString));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(m => m.TransactionDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(m => m.TransactionDate <= toDate.Value.AddDays(1));
            }

            var transactions = await query
                .OrderByDescending(m => m.TransactionDate)
                .ToListAsync();

            ViewBag.SearchString = searchString;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(transactions);
        }

        public async Task<IActionResult> Details(int id)
        {
            var transaction = await _context.MomoTransactions
                .Include(m => m.Order)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel(string searchString, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.MomoTransactions
                .Include(m => m.Order)
                .AsQueryable();

            // Áp dụng filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(m => 
                    m.OrderCode.Contains(searchString) || 
                    m.TransactionId.Contains(searchString) ||
                    m.RequestId.Contains(searchString));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(m => m.TransactionDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(m => m.TransactionDate <= toDate.Value.AddDays(1));
            }

            var transactions = await query
                .OrderByDescending(m => m.TransactionDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("MOMO Transactions");

                // Add headers
                worksheet.Cell(1, 1).Value = "Mã đơn hàng";
                worksheet.Cell(1, 2).Value = "Mã giao dịch MOMO";
                worksheet.Cell(1, 3).Value = "Số tiền";
                worksheet.Cell(1, 4).Value = "Thời gian";
                worksheet.Cell(1, 5).Value = "Phương thức";
                worksheet.Cell(1, 6).Value = "Trạng thái";

                // Add data
                int row = 2;
                foreach (var transaction in transactions)
                {
                    worksheet.Cell(row, 1).Value = transaction.OrderCode;
                    worksheet.Cell(row, 2).Value = transaction.TransactionId;
                    worksheet.Cell(row, 3).Value = transaction.Amount;
                    worksheet.Cell(row, 4).Value = transaction.TransactionDate.ToString("dd/MM/yyyy HH:mm:ss");
                    worksheet.Cell(row, 5).Value = transaction.PayType;
                    worksheet.Cell(row, 6).Value = transaction.ResultCode == 0 ? "Thành công" : "Thất bại";
                    row++;
                }

                // Format the cells
                var range = worksheet.Range(1, 1, row - 1, 6);
                range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Columns().AdjustToContents();

                // Generate the file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"momo_transactions_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRealtimeNotifications()
        {
            var latestTransactions = await _context.MomoTransactions
                .OrderByDescending(m => m.TransactionDate)
                .Take(5)
                .Select(m => new
                {
                    m.OrderCode,
                    m.Amount,
                    m.TransactionDate,
                    m.ResultCode
                })
                .ToListAsync();

            return Json(latestTransactions);
        }
    }
} 