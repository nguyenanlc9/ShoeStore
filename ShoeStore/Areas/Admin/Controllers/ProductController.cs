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

        // Thêm phương thức helper để xử lý upload nhiều file
        private async Task<string> UploadFiles(List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return null;

            var imageUrls = new List<string>();
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    imageUrls.Add("/images/products/" + uniqueFileName);
                }
            }

            return string.Join(",", imageUrls);
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index()
        {
            ViewData["ActiveMenu"] = "product";

            var products = await _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Include(p => p.ProductSizeStocks)
                .ThenInclude(pss => pss.Size)
                .Include(p => p.OrderDetails)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.ProductId)
                .ToListAsync();

            return View(products);
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
                .Include(p => p.Brands)
                .Include(p => p.ProductSizeStocks)
                .ThenInclude(pss => pss.Size)
                .Include(p => p.ProductImages)
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
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name");
            return View();
        }

        // POST: Admin/Product/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile mainImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy thông tin user hiện tại
                    var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");
                    if (currentUser != null)
                    {
                        product.CreatedBy = currentUser.Username;
                        product.CreatedDate = DateTime.Now;
                    }

                    // Xử lý upload ảnh đại diện
                    if (mainImage != null && mainImage.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(mainImage.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", uniqueFileName);
                        var relativePath = "/images/products/" + uniqueFileName;

                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await mainImage.CopyToAsync(stream);
                        }
                        
                        product.ImagePath = relativePath;
                    }

                    // Thêm sản phẩm
                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    // Lưu lịch sử tạo sản phẩm
                    var history = new ProductHistory
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        DiscountPrice = product.DiscountPrice,
                        Description = product.Description,
                        CategoryId = product.CategoryId,
                        BrandId = product.BrandId,
                        ProductCode = product.ProductCode,
                        IsHot = product.IsHot,
                        IsNew = product.IsNew,
                        IsSale = product.IsSale,
                        Status = product.Status,
                        ImagePath = product.ImagePath,
                        ModifiedBy = currentUser?.Username,
                        ModifiedDate = DateTime.Now,
                        Action = "Create"
                    };
                    _context.ProductHistories.Add(history);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo sản phẩm: " + ex.Message);
                }
            }
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .Include(p => p.ProductSizeStocks)
                .ThenInclude(pss => pss.Size)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            ViewBag.Sizes = await _context.Size.ToListAsync();

            return View(product);
        }

        // POST: Admin/Product/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile mainImage)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            try
            {
                // Lấy sản phẩm hiện tại từ database
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Lấy thông tin user hiện tại
                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");

                // Cập nhật thông tin sản phẩm
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.DiscountPrice = product.DiscountPrice;
                existingProduct.BrandId = product.BrandId;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.ProductCode = product.ProductCode;
                existingProduct.IsHot = product.IsHot;
                existingProduct.IsNew = product.IsNew;
                existingProduct.IsSale = product.DiscountPrice > 0;
                existingProduct.Status = product.Status;

                // Chỉ cập nhật UpdatedBy và UpdatedDate
                if (currentUser != null)
                {
                    existingProduct.UpdatedBy = currentUser.Username;
                    existingProduct.UpdatedDate = DateTime.Now;
                }

                // Xử lý upload ảnh đại diện
                if (mainImage != null && mainImage.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existingProduct.ImagePath))
                    {
                        var oldMainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingProduct.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldMainPath))
                        {
                            System.IO.File.Delete(oldMainPath);
                        }
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(mainImage.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", uniqueFileName);
                    var relativePath = "/images/products/" + uniqueFileName;

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                    
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await mainImage.CopyToAsync(stream);
                    }
                    
                    existingProduct.ImagePath = relativePath;
                }

                // Lưu lịch sử cập nhật sản phẩm
                var history = new ProductHistory
                {
                    ProductId = existingProduct.ProductId,
                    Name = existingProduct.Name,
                    Price = existingProduct.Price,
                    DiscountPrice = existingProduct.DiscountPrice,
                    Description = existingProduct.Description,
                    CategoryId = existingProduct.CategoryId,
                    BrandId = existingProduct.BrandId,
                    ProductCode = existingProduct.ProductCode,
                    IsHot = existingProduct.IsHot,
                    IsNew = existingProduct.IsNew,
                    IsSale = existingProduct.IsSale,
                    Status = existingProduct.Status,
                    ImagePath = existingProduct.ImagePath,
                    ModifiedBy = currentUser?.Username,
                    ModifiedDate = DateTime.Now,
                    Action = "Update"
                };
                _context.ProductHistories.Add(history);

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
            }

            ViewData["BrandId"] = new SelectList(_context.Brands, "BrandId", "Name", product.BrandId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
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

        // Xóa ảnh sản phẩm
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            try
            {
                var image = await _context.ProductImages
                    .Include(pi => pi.Product)
                    .FirstOrDefaultAsync(pi => pi.ImageId == imageId);

                if (image == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ảnh" });
                }

                // Xóa file ảnh từ thư mục
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Nếu là ảnh chính, cập nhật ImagePath của sản phẩm
                if (image.IsMainImage && image.Product != null)
                {
                    image.Product.ImagePath = null;
                }

                // Xóa record trong database
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa ảnh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa ảnh: " + ex.Message });
            }
        }

        // Thêm action để quản lý ảnh sản phẩm
        [HttpGet]
        public async Task<IActionResult> ManageImages(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImages(int productId, List<IFormFile> images)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                foreach (var image in images)
                {
                    if (image != null && image.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        var filePath = Path.Combine("wwwroot/images/products", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = productId,
                            ImagePath = "/images/products/" + fileName,
                            CreatedAt = DateTime.Now
                        };

                        _context.ProductImages.Add(productImage);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetMainImage([FromBody] SetMainImageRequest request)
        {
            try
            {
                var image = await _context.ProductImages
                    .Include(pi => pi.Product)
                    .FirstOrDefaultAsync(pi => pi.ImageId == request.ImageId);

                if (image == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ảnh" });
                }

                image.Product.ImagePath = image.ImagePath;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage([FromBody] DeleteImageRequest request)
        {
            try
            {
                var image = await _context.ProductImages.FindAsync(request.ImageId);
                if (image == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy ảnh" });
                }

                var filePath = Path.Combine("wwwroot", image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/Product/History/5
        public async Task<IActionResult> History(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Categories)
                .Include(p => p.Brands)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Lấy lịch sử thay đổi của sản phẩm
            var histories = await _context.ProductHistories
                .Where(h => h.ProductId == id)
                .OrderByDescending(h => h.ModifiedDate)
                .ToListAsync();

            ViewBag.Histories = histories;

            return View(product);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        public class SetMainImageRequest
        {
            public int ImageId { get; set; }
        }

        public class DeleteImageRequest
        {
            public int ImageId { get; set; }
        }
    }
}
