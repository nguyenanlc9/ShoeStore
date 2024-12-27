using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Filters;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class ProductSizeStockController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductSizeStockController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stocks = await _context.ProductSizeStocks
                .Include(p => p.Product)
                .Include(p => p.Size)
                .ToListAsync();
            return View(stocks);
        }

        public IActionResult Create()
        {
            ViewBag.Products = new SelectList(_context.Products
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName");

            ViewBag.Sizes = new SelectList(_context.Size
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,SizeID,StockQuantity")] ProductSizeStock stock)
        {
            try
            {
                // Thêm logging
                Console.WriteLine($"Creating stock - ProductID: {stock.ProductID}, SizeID: {stock.SizeID}, Quantity: {stock.StockQuantity}");
                
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
                    _context.Add(stock);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Thêm số lượng tồn kho thành công";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating stock: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
            }

            // Nếu có lỗi, tạo lại SelectList
            ViewBag.Products = new SelectList(_context.Products
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName", stock.ProductID);

            ViewBag.Sizes = new SelectList(_context.Size
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue", stock.SizeID);

            return View(stock);
        }

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
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName", stock.ProductID);

            ViewBag.Sizes = new SelectList(_context.Size
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue", stock.SizeID);

            return View(stock);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductSizeStockID,ProductID,SizeID,StockQuantity")] ProductSizeStock stock)
        {
            if (id != stock.ProductSizeStockID)
            {
                return NotFound();
            }

            try
            {
                Console.WriteLine($"Editing stock - ProductID: {stock.ProductID}, SizeID: {stock.SizeID}, Quantity: {stock.StockQuantity}");

                if (!ModelState.IsValid)
                {
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Model Error: {modelError.ErrorMessage}");
                    }
                }

                // Kiểm tra xem combination mới đã tồn tại chưa
                var existingStock = await _context.ProductSizeStocks
                    .FirstOrDefaultAsync(s => s.ProductID == stock.ProductID && 
                                            s.SizeID == stock.SizeID && 
                                            s.ProductSizeStockID != id);

                if (existingStock != null)
                {
                    ModelState.AddModelError("", "Sản phẩm này đã có size này trong kho!");
                }
                else
                {
                    _context.Update(stock);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật số lượng tồn kho thành công";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error editing stock: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
            }

            ViewBag.Products = new SelectList(_context.Products
                .Select(p => new
                {
                    ProductID = p.ProductId,
                    DisplayName = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayName", stock.ProductID);

            ViewBag.Sizes = new SelectList(_context.Size
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayValue = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayValue", stock.SizeID);

            return View(stock);
        }

        private bool ProductSizeStockExists(int id)
        {
            return _context.ProductSizeStocks.Any(e => e.ProductSizeStockID == id);
        }
    }
}
