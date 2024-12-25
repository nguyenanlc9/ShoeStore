using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Requset;
using ShoeStore.Utils;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Thêm phương thức helper để xử lý upload file
        private async Task<string> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/images/products/" + uniqueFileName;
        }


        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            ViewData["ActiveMenu"] = "product";

            var applicationDbContext = _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .OrderByDescending(p => p.ProductId)                                           
                ;

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Product/Create
        public IActionResult Create()
        {

            // Kiểm tra xem có brands nào không
            var brands = _context.Brands.ToList();
            if (brands.Count == 0)
            {
                // Log hoặc thông báo nếu không có brands
                System.Diagnostics.Debug.WriteLine("No brands found in database!");
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name");
            ViewData["BrandId"] = new SelectList(_context.Brands.OrderBy(b => b.DisplayOrder), "BrandId", "Name");
            return View();
        }

        // POST: Admin/Product/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,CategoryId,BrandId,Name,Price,Description,DiscountPrice,Status")] Product product, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Loại bỏ thẻ <p> và </p> khỏi Description nếu có
                var description = product.Description?.Trim();
                if (!string.IsNullOrEmpty(description) && description.StartsWith("<p>") && description.EndsWith("</p>"))
                {
                    description = description.Substring(3, description.Length - 7);
                }

                product.Description = description;
                product.UpdatedDate = DateTime.Now;
                product.UpdatedBy = HttpContext.Session.Get<User>("AdminUserInfo")?.Username ?? string.Empty;

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    product.ImagePath = await UploadFile(ImageFile);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            return View(product);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Thêm dòng này để set SelectList cho Brand
            ViewData["BrandId"] = new SelectList(_context.Brands.OrderBy(b => b.DisplayOrder), "BrandId", "Name");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);

            return View(product);
        }

        // POST: Admin/Product/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,CategoryId,BrandId,Name,Price,Description,DiscountPrice,Status,UpdatedDate,UpdatedBy,ImagePath")] Product product, IFormFile? ImageFile)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.FindAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    existingProduct.Name = product.Name;
                    existingProduct.BrandId = product.BrandId;
                    existingProduct.Price = product.Price;
                    existingProduct.DiscountPrice = product.DiscountPrice;
                    existingProduct.Description = product.Description;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.Status = product.Status;
                    existingProduct.UpdatedDate = DateTime.Now;
                    existingProduct.UpdatedBy = HttpContext.Session.Get<User>("AdminUserInfo")?.Username ?? string.Empty;

                    // Chỉ cập nhật ảnh khi có file mới được upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingProduct.ImagePath))
                        {
                            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        // Upload và lưu đường dẫn ảnh mới
                        existingProduct.ImagePath = await UploadFile(ImageFile);
                    }
                    else
                    {
                        // Giữ nguyên đường dẫn ảnh cũ
                        existingProduct.ImagePath = existingProduct.ImagePath;
                    }

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", product.CategoryId);
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            return View(product);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
