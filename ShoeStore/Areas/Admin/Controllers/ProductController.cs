using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoesStore.Models;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Requset;
using ShoesStore.Utils;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthentication]
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
        public async Task<IActionResult> Create(ProductDTO productDTO)
        {
            var userInfo = HttpContext.Session.Get<AdminUser>("userInfo");
            var userName = "";
            if (userInfo != null) userName = userInfo.Username;
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    CategoryId = productDTO.CategoryId,
                    BrandId = productDTO.BrandId,
                    Name = productDTO.Name,
                    Price = productDTO.Price,
                    DiscountPrice = productDTO.DiscountPrice,
                    Description = productDTO.Description,
                    StockQuantity = productDTO.StockQuantity,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = userName
                };

                if (productDTO.ImagePath != null)
                {
                    product.ImagePath = await UploadFile(productDTO.ImagePath);
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", productDTO.CategoryId);
            return View(productDTO);
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
        public async Task<IActionResult> Edit(int id, ProductDTO productDTO)
            {
            if (id != productDTO.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userInfo = HttpContext.Session.Get<AdminUser>("userInfo");
                    var username = userInfo != null ? userInfo.Username : "";

                    var existingProduct = await _context.Products.FindAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    existingProduct.Name = productDTO.Name;
                    existingProduct.BrandId = productDTO.BrandId;
                    existingProduct.Price = productDTO.Price;
                    existingProduct.DiscountPrice = productDTO.DiscountPrice;
                    existingProduct.Description = productDTO.Description;
                    existingProduct.StockQuantity = productDTO.StockQuantity;
                    existingProduct.CategoryId = productDTO.CategoryId;
                    existingProduct.UpdatedDate = DateTime.Now;
                    existingProduct.UpdatedBy = username;

                    if (productDTO.ImagePath != null && productDTO.ImagePath.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingProduct.ImagePath))
                        {
                            string oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        existingProduct.ImagePath = await UploadFile(productDTO.ImagePath);
                    }

                    _context.Update(existingProduct);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(productDTO.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Nếu ModelState không hợp lệ, trả lại View với danh sách Category
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", productDTO.CategoryId);
            return View(productDTO);
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
