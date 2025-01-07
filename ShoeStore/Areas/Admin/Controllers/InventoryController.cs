using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Filters;
using Microsoft.AspNetCore.Http;
using ShoeStore.Utils;
using ClosedXML.Excel;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Inventory
        public async Task<IActionResult> Index()
        {
            var stocks = await _context.ProductSizeStocks
                .Include(p => p.Product)
                .Include(p => p.Size)
                .OrderBy(p => p.Product.Name)
                .ThenBy(p => p.Size.SizeValue)
                .Select(s => new ProductSizeStock
                {
                    ProductSizeStockID = s.ProductSizeStockID,
                    ProductID = s.ProductID,
                    SizeID = s.SizeID,
                    Product = s.Product,
                    Size = s.Size,
                    StockQuantity = s.StockQuantity,
                    CreatedBy = s.CreatedBy,
                    CreatedDate = s.CreatedDate,
                    UpdatedBy = s.UpdatedBy,
                    UpdatedDate = s.UpdatedDate
                })
                .ToListAsync();
            return View(stocks);
        }

        // GET: Admin/Inventory/Sizes
        public async Task<IActionResult> Sizes()
        {
            var sizes = await _context.Size
                .OrderBy(s => s.SizeValue)
                .ToListAsync();
            return View(sizes);
        }

        // GET: Admin/Inventory/CreateSize
        public IActionResult CreateSize()
        {
            return View();
        }

        // POST: Admin/Inventory/CreateSize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSize([Bind("SizeValue")] Size size)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra size đã tồn tại chưa
                if (await _context.Size.AnyAsync(s => s.SizeValue == size.SizeValue))
                {
                    ModelState.AddModelError("", "Size này đã tồn tại!");
                    return View(size);
                }

                _context.Add(size);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm size mới thành công";
                return RedirectToAction(nameof(Sizes));
            }
            return View(size);
        }

        // GET: Admin/Inventory/EditSize/5
        public async Task<IActionResult> EditSize(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var size = await _context.Size.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }
            return View(size);
        }

        // POST: Admin/Inventory/EditSize/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSize(int id, [Bind("SizeID,SizeValue")] Size size)
        {
            if (id != size.SizeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra size đã tồn tại chưa
                    if (await _context.Size.AnyAsync(s => s.SizeValue == size.SizeValue && s.SizeID != id))
                    {
                        ModelState.AddModelError("", "Size này đã tồn tại!");
                        return View(size);
                    }

                    _context.Update(size);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật size thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Size.AnyAsync(e => e.SizeID == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Sizes));
            }
            return View(size);
        }

        // GET: Admin/Inventory/Create
        public IActionResult Create()
        {
            ViewBag.Products = new SelectList(_context.Products
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName");

            ViewBag.Sizes = new SelectList(_context.Size
                .OrderBy(s => s.SizeValue)
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue");

            return View();
        }

        // POST: Admin/Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,SizeID,StockQuantity")] ProductSizeStock stock)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Model Error: {modelError.ErrorMessage}");
                    }
                }

                // Kiểm tra xem combination đã tồn tại chưa
                var existingStock = await _context.ProductSizeStocks
                    .FirstOrDefaultAsync(s => s.ProductID == stock.ProductID && s.SizeID == stock.SizeID);

                if (existingStock != null)
                {
                    ModelState.AddModelError("", "Sản phẩm này đã có size này trong kho!");
                }
                else
                {
                    // Lấy thông tin sản phẩm
                    var product = await _context.Products
                        .Include(p => p.ProductSizeStocks)
                        .FirstOrDefaultAsync(p => p.ProductId == stock.ProductID);

                    if (product != null)
                    {
                        var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");
                        stock.CreatedBy = currentUser.Username;
                        stock.CreatedDate = DateTime.Now;
                        stock.UpdatedBy = currentUser.Username;
                        stock.UpdatedDate = DateTime.Now;

                        _context.Add(stock);
                        await _context.SaveChangesAsync();

                        // Cập nhật trạng thái sản phẩm
                        var totalStock = product.ProductSizeStocks.Sum(s => s.StockQuantity) + stock.StockQuantity;
                        if (totalStock > 0 && product.Status == ProductStatus.OutOfStock)
                        {
                            product.Status = ProductStatus.Available;
                            await _context.SaveChangesAsync();
                        }
                    }

                    TempData["SuccessMessage"] = "Nhập kho thành công";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
            }

            // Nếu có lỗi, tạo lại SelectList
            ViewBag.Products = new SelectList(_context.Products
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName", stock.ProductID);

            ViewBag.Sizes = new SelectList(_context.Size
                .OrderBy(s => s.SizeValue)
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue", stock.SizeID);

            return View(stock);
        }

        // GET: Admin/Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stock = await _context.ProductSizeStocks
                .Include(p => p.Product)
                .Include(p => p.Size)
                .FirstOrDefaultAsync(m => m.ProductSizeStockID == id);

            if (stock == null)
            {
                return NotFound();
            }

            ViewBag.Products = new SelectList(_context.Products
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName", stock.ProductID);

            ViewBag.Sizes = new SelectList(_context.Size
                .OrderBy(s => s.SizeValue)
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue", stock.SizeID);

            return View(stock);
        }

        // POST: Admin/Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductSizeStockID,ProductID,SizeID,StockQuantity")] ProductSizeStock stock)
        {
            if (id != stock.ProductSizeStockID)
            {
                return NotFound();
            }

            // Kiểm tra số lượng tồn kho
            if (stock.StockQuantity < 0)
            {
                ModelState.AddModelError("StockQuantity", "Số lượng tồn kho không thể âm");
                PrepareViewBagForEdit(stock);
                return View(stock);
            }

            // Lấy thông tin stock hiện tại từ database
            var currentStock = await _context.ProductSizeStocks
                .FirstOrDefaultAsync(s => s.ProductSizeStockID == id);

            if (currentStock == null)
            {
                return NotFound();
            }

            try
            {
                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Lưu lịch sử nếu số lượng thay đổi
                if (currentStock.StockQuantity != stock.StockQuantity)
                {
                    _context.ProductSizeStockHistories.Add(new ProductSizeStockHistory
                    {
                        ProductId = stock.ProductID,
                        SizeId = stock.SizeID,
                        OldQuantity = currentStock.StockQuantity,
                        NewQuantity = stock.StockQuantity,
                        ModifiedBy = currentUser.Username,
                        ModifiedDate = DateTime.Now,
                        Action = "Update"
                    });
                }

                // Cập nhật thông tin stock
                currentStock.StockQuantity = stock.StockQuantity;
                currentStock.UpdatedBy = currentUser.Username;
                currentStock.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Cập nhật trạng thái sản phẩm
                await UpdateProductStatus(stock.ProductID);

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Cập nhật số lượng tồn kho thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật: " + ex.Message);
                PrepareViewBagForEdit(stock);
                return View(stock);
            }
        }

        private async Task UpdateProductStatus(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizeStocks)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product != null)
            {
                var totalStock = product.ProductSizeStocks.Sum(s => s.StockQuantity);
                if (totalStock > 0 && product.Status == ProductStatus.OutOfStock)
                {
                    product.Status = ProductStatus.Available;
                }
                else if (totalStock == 0 && product.Status == ProductStatus.Available)
                {
                    product.Status = ProductStatus.OutOfStock;
                }
                await _context.SaveChangesAsync();
            }
        }

        private void PrepareViewBagForEdit(ProductSizeStock stock)
        {
            ViewBag.Products = new SelectList(_context.Products
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName", stock.ProductID);

            ViewBag.Sizes = new SelectList(_context.Size
                .OrderBy(s => s.SizeValue)
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue", stock.SizeID);
        }

        // POST: Admin/Inventory/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var stock = await _context.ProductSizeStocks
                .Include(p => p.Product)
                .FirstOrDefaultAsync(s => s.ProductSizeStockID == id);

            if (stock == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin tồn kho" });
            }

            // Kiểm tra số lượng tồn kho
            if (stock.StockQuantity > 5)
            {
                return Json(new { success = false, message = "Không thể xóa tồn kho vì số lượng còn nhiều hơn 5" });
            }

            try
            {
                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");

                // Lưu lịch sử xóa
                _context.ProductSizeStockHistories.Add(new ProductSizeStockHistory
                {
                    ProductId = stock.ProductID,
                    SizeId = stock.SizeID,
                    OldQuantity = stock.StockQuantity,
                    NewQuantity = 0,
                    ModifiedBy = currentUser?.Username,
                    ModifiedDate = DateTime.Now,
                    Action = "Delete"
                });

                _context.ProductSizeStocks.Remove(stock);
                await _context.SaveChangesAsync();

                // Cập nhật trạng thái sản phẩm
                await UpdateProductStatus(stock.ProductID);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: Admin/Inventory/DeleteSize/5
        [HttpPost]
        public async Task<IActionResult> DeleteSize(int id)
        {
            var size = await _context.Size.FindAsync(id);
            if (size == null)
            {
                return Json(new { success = false, message = "Không tìm thấy size" });
            }

            try
            {
                _context.Size.Remove(size);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: Admin/Inventory/History/5
        public async Task<IActionResult> History(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSizeStock = await _context.ProductSizeStocks
                .Include(p => p.Product)
                .Include(p => p.Size)
                .FirstOrDefaultAsync(p => p.ProductSizeStockID == id);

            if (productSizeStock == null)
            {
                return NotFound();
            }

            var history = await _context.ProductSizeStockHistories
                .Include(h => h.Product)
                .Include(h => h.Size)
                .Where(h => h.ProductId == productSizeStock.ProductID && h.SizeId == productSizeStock.SizeID)
                .OrderByDescending(h => h.ModifiedDate)
                .ToListAsync();

            ViewBag.ProductSizeStock = productSizeStock;
            return View(history);
        }

        // GET: Admin/Inventory/ExportExcel
        public async Task<IActionResult> ExportExcel()
        {
            var stocks = await _context.ProductSizeStocks
                .Include(p => p.Product)
                .Include(p => p.Size)
                .OrderBy(p => p.Product.Name)
                .ThenBy(p => p.Size.SizeValue)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Stock");

                // Đặt tên cho các cột
                worksheet.Cell(1, 1).Value = "Mã SP";
                worksheet.Cell(1, 2).Value = "Tên sản phẩm";
                worksheet.Cell(1, 3).Value = "Size";
                worksheet.Cell(1, 4).Value = "Số lượng tồn";
                worksheet.Cell(1, 5).Value = "Ngày tạo";
                worksheet.Cell(1, 6).Value = "Người tạo";
                worksheet.Cell(1, 7).Value = "Ngày cập nhật";
                worksheet.Cell(1, 8).Value = "Người cập nhật";

                // Style cho header
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Fill dữ liệu
                int row = 2;
                foreach (var item in stocks)
                {
                    worksheet.Cell(row, 1).Value = item.Product?.ProductCode;
                    worksheet.Cell(row, 2).Value = item.Product?.Name;
                    worksheet.Cell(row, 3).Value = item.Size?.SizeValue;
                    worksheet.Cell(row, 4).Value = item.StockQuantity;
                    worksheet.Cell(row, 5).Value = item.CreatedDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 6).Value = item.CreatedBy;
                    worksheet.Cell(row, 7).Value = item.UpdatedDate?.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 8).Value = item.UpdatedBy;

                    // Style cho cột số lượng
                    worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                    
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Save to memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    string excelName = $"Stock_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
        }

        private bool ProductSizeStockExists(int id)
        {
            return _context.ProductSizeStocks.Any(e => e.ProductSizeStockID == id);
        }
    }
} 