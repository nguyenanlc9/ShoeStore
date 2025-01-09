using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Data;
using ShoeStore.Models;
using System.Text.Json;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BlogController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Blog
        public async Task<IActionResult> Index()
        {
            return View(await _context.Blogs.Include(b => b.BlogImages).ToListAsync());
        }

        // GET: Admin/Blog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Include(b => b.BlogImages)
                .FirstOrDefaultAsync(m => m.BlogId == id);
            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // GET: Admin/Blog/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content,Author,IsVisible")] Blog blog, IFormFile? thumbnailImage)
        {
            if (ModelState.IsValid)
            {
                if (thumbnailImage != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnailImage.FileName);
                    string relativePath = Path.Combine("images", "blogs", fileName);
                    string fullPath = Path.Combine(wwwRootPath, relativePath);

                    // Tạo thư mục nếu chưa tồn tại
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                    using (var fileStream = new FileStream(fullPath, FileMode.Create))
                    {
                        await thumbnailImage.CopyToAsync(fileStream);
                    }

                    blog.ThumbnailImage = $"/{relativePath.Replace("\\", "/")}";
                }

                _context.Add(blog);
                await _context.SaveChangesAsync();

                // Cập nhật BlogId cho các ảnh trong nội dung
                var contentImages = await _context.BlogImages
                    .Where(bi => bi.BlogId == 0 && bi.IsContentImage)
                    .ToListAsync();

                foreach (var image in contentImages)
                {
                    image.BlogId = blog.BlogId;
                    _context.Update(image);
                }
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(blog);
        }

        // GET: Admin/Blog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null)
            {
                return NotFound();
            }
            return View(blog);
        }

        // POST: Admin/Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BlogId,Title,Content,Author,IsVisible,CreatedDate")] Blog blog, IFormFile? thumbnailImage)
        {
            if (id != blog.BlogId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBlog = await _context.Blogs.AsNoTracking().FirstOrDefaultAsync(b => b.BlogId == id);
                    blog.ThumbnailImage = existingBlog.ThumbnailImage;

                    if (thumbnailImage != null)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(thumbnailImage.FileName);
                        string relativePath = Path.Combine("images", "blogs", fileName);
                        string fullPath = Path.Combine(wwwRootPath, relativePath);

                        // Tạo thư mục nếu chưa tồn tại
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                        if (!string.IsNullOrEmpty(blog.ThumbnailImage))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, blog.ThumbnailImage.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        using (var fileStream = new FileStream(fullPath, FileMode.Create))
                        {
                            await thumbnailImage.CopyToAsync(fileStream);
                        }

                        blog.ThumbnailImage = $"/{relativePath.Replace("\\", "/")}";
                    }

                    _context.Update(blog);
                    await _context.SaveChangesAsync();

                    // Cập nhật BlogId cho các ảnh trong nội dung
                    var contentImages = await _context.BlogImages
                        .Where(bi => bi.BlogId == 0 && bi.IsContentImage)
                        .ToListAsync();

                    foreach (var image in contentImages)
                    {
                        image.BlogId = blog.BlogId;
                        _context.Update(image);
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogExists(blog.BlogId))
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
            return View(blog);
        }

        // GET: Admin/Blog/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .FirstOrDefaultAsync(m => m.BlogId == id);
            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // POST: Admin/Blog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var blog = await _context.Blogs.Include(b => b.BlogImages).FirstOrDefaultAsync(b => b.BlogId == id);
            if (blog != null)
            {
                // Xóa ảnh đại diện
                if (!string.IsNullOrEmpty(blog.ThumbnailImage))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, blog.ThumbnailImage.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Xóa các ảnh trong nội dung
                if (blog.BlogImages != null)
                {
                    foreach (var image in blog.BlogImages)
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                }

                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload == null || upload.Length == 0)
            {
                return Json(new { error = new { message = "No file uploaded." } });
            }

            try
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(upload.FileName);
                string relativePath = Path.Combine("images", "blogs", "content", fileName);
                string fullPath = Path.Combine(wwwRootPath, relativePath);

                // Tạo thư mục nếu chưa tồn tại
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await upload.CopyToAsync(fileStream);
                }

                var url = $"/{relativePath.Replace("\\", "/")}";

                // Lưu thông tin vào bảng BlogImage
                var blogImage = new BlogImage
                {
                    ImageUrl = url,
                    IsContentImage = true
                };

                _context.BlogImages.Add(blogImage);
                await _context.SaveChangesAsync();

                // Trả về response theo định dạng mà CKEditor yêu cầu
                return Json(new
                {
                    uploaded = 1,
                    fileName = fileName,
                    url = url
                });
            }
            catch (Exception ex)
            {
                return Json(new { uploaded = 0, error = new { message = ex.Message } });
            }
        }

        private bool BlogExists(int id)
        {
            return _context.Blogs.Any(e => e.BlogId == id);
        }
    }
} 