using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductSizeStockController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductSizeStockController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ProductSizeStock
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ProductSizeStock.Include(p => p.Product).Include(p => p.Size);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admin/ProductSizeStock/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSizeStock = await _context.ProductSizeStock
                .Include(p => p.Product)
                .Include(p => p.Size)
                .FirstOrDefaultAsync(m => m.ProductSizeStockID == id);
            if (productSizeStock == null)
            {
                return NotFound();
            }

            return View(productSizeStock);
        }

        // GET: Admin/ProductSizeStock/Create
        public IActionResult Create()
        {
            // Lấy danh sách sản phẩm với tên
            ViewData["ProductID"] = new SelectList(_context.Products
                .Select(p => new 
                {
                    ProductID = p.ProductId,
                    DisplayText = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayText");

            // Lấy danh sách size với giá trị
            ViewData["SizeID"] = new SelectList(_context.Size
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayText = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayText");

            return View();
        }

        // POST: Admin/ProductSizeStock/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductSizeStockID,ProductID,SizeID,StockQuantity")] ProductSizeStock productSizeStock)
        {
            if (ModelState.IsValid)
            {
                _context.Add(productSizeStock);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductId", "ProductId", productSizeStock.ProductID);
            ViewData["SizeID"] = new SelectList(_context.Set<Size>(), "SizeID", "SizeID", productSizeStock.SizeID);
            return View(productSizeStock);
        }

        // GET: Admin/ProductSizeStock/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSizeStock = await _context.ProductSizeStock.FindAsync(id);
            if (productSizeStock == null)
            {
                return NotFound();
            }

            // Lấy danh sách sản phẩm với tên
            ViewData["ProductID"] = new SelectList(_context.Products
                .Select(p => new 
                {
                    ProductID = p.ProductId,
                    DisplayText = $"{p.Name} - {p.Price:N0}đ"
                }), "ProductID", "DisplayText", productSizeStock.ProductID);

            // Lấy danh sách size với giá trị
            ViewData["SizeID"] = new SelectList(_context.Size
                .Select(s => new
                {
                    SizeID = s.SizeID,
                    DisplayText = $"Size {s.SizeValue}"
                }), "SizeID", "DisplayText", productSizeStock.SizeID);

            return View(productSizeStock);
        }

        // POST: Admin/ProductSizeStock/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductSizeStockID,ProductID,SizeID,StockQuantity")] ProductSizeStock productSizeStock)
        {
            if (id != productSizeStock.ProductSizeStockID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(productSizeStock);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductSizeStockExists(productSizeStock.ProductSizeStockID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductId", "ProductId", productSizeStock.ProductID);
            ViewData["SizeID"] = new SelectList(_context.Set<Size>(), "SizeID", "SizeID", productSizeStock.SizeID);
            return View(productSizeStock);
        }

        // GET: Admin/ProductSizeStock/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productSizeStock = await _context.ProductSizeStock
                .Include(p => p.Product)
                .Include(p => p.Size)
                .FirstOrDefaultAsync(m => m.ProductSizeStockID == id);
            if (productSizeStock == null)
            {
                return NotFound();
            }

            return View(productSizeStock);
        }

        // POST: Admin/ProductSizeStock/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productSizeStock = await _context.ProductSizeStock.FindAsync(id);
            if (productSizeStock != null)
            {
                _context.ProductSizeStock.Remove(productSizeStock);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductSizeStockExists(int id)
        {
            return _context.ProductSizeStock.Any(e => e.ProductSizeStockID == id);
        }
    }
}
