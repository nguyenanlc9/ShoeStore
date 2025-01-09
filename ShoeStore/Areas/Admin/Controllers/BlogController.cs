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

    public class BlogController : Controller

    {

        private readonly ApplicationDbContext _context;



        public BlogController(ApplicationDbContext context)

        {

            _context = context;

        }

        // Thêm phương thức helper để xử lý upload nhiều file

        private async Task<string> UploadFiles(List<IFormFile> files)

        {

            if (files == null || !files.Any())

                return null;



            var imageUrls = new List<string>();

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "blogs");



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



                    imageUrls.Add("/images/blogs/" + uniqueFileName);

                }

            }



            return string.Join(",", imageUrls);

        }



        // GET: Admin/Blog

        public async Task<IActionResult> Index()

        {

            return View(await _context.Blogs.OrderBy(s => s.Sort).ToListAsync());

        }



        // GET: Admin/Blog/Details/5

        public async Task<IActionResult> Details(int? id)

        {

            if (id == null)

            {

                return NotFound();

            }



            var blog = await _context.Blogs

                .FirstOrDefaultAsync(m => m.Blog_ID == id);

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

        // To protect from overposting attacks, enable the specific properties you want to bind to.

        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("Title,Description,Status,Sort")] Blog blog, IFormFile mainImage)

        {

            try

            {

                // Lấy thông tin user hiện tại

                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");

                if (currentUser != null)

                {

                    blog.CreatedBy = currentUser.Username;

                    blog.UpdatedBy = currentUser.Username;

                }

                else

                {

                    blog.CreatedBy = "Admin";

                    blog.UpdatedBy = "Admin";

                }



                blog.CreatedDate = DateTime.Now;

                blog.UpdatedDate = DateTime.Now;

                blog.BlogImages = new List<BlogImage>();



                // Xử lý upload ảnh

                if (mainImage != null && mainImage.Length > 0)

                {

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(mainImage.FileName);

                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "blogs");

                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    var relativePath = "/images/blogs/" + uniqueFileName;



                    // Đảm bảo thư mục tồn tại

                    if (!Directory.Exists(uploadsFolder))

                    {

                        Directory.CreateDirectory(uploadsFolder);

                    }



                    using (var stream = new FileStream(filePath, FileMode.Create))

                    {

                        await mainImage.CopyToAsync(stream);

                    }



                    blog.Img = relativePath;

                }



                // Thêm blog vào database

                _context.Blogs.Add(blog);

                await _context.SaveChangesAsync();



                // Thêm ảnh vào bảng BlogImages nếu có

                if (!string.IsNullOrEmpty(blog.Img))

                {

                    var blogImage = new BlogImage

                    {

                        Blog_ID = blog.Blog_ID,

                        Img = blog.Img,

                        IsMainImage = true,

                        CreatedAt = DateTime.Now

                    };

                    _context.BlogImages.Add(blogImage);

                    await _context.SaveChangesAsync();

                }



                TempData["SuccessMessage"] = "Thêm bài viết thành công!";

                return RedirectToAction(nameof(Index));

            }

            catch (DbUpdateException ex)

            {

                var innerException = ex.InnerException;

                while (innerException != null)

                {

                    ModelState.AddModelError("", $"Inner Exception: {innerException.Message}");

                    innerException = innerException.InnerException;

                }

                return View(blog);

            }

            catch (Exception ex)

            {

                ModelState.AddModelError("", $"Có lỗi xảy ra khi tạo bài viết: {ex.Message}");

                if (ex.InnerException != null)

                {

                    ModelState.AddModelError("", $"Inner Exception: {ex.InnerException.Message}");

                }

                return View(blog);

            }

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

        // To protect from overposting attacks, enable the specific properties you want to bind to.

        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, [Bind("Blog_ID,Title,Img,Description,Status,Sort,CreatedDate,CreatedBy,UpdatedDate,UpdatedBy")] Blog blog, IFormFile mainImage)

        {

            if (id != blog.Blog_ID)

            {

                return NotFound();

            }



            try

            {

                // Lấy sản phẩm hiện tại từ database

                var existingBlog = await _context.Blogs.FindAsync(id);

                if (existingBlog == null)

                {

                    return NotFound();

                }



                // Lấy thông tin user hiện tại

                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");



                // Cập nhật thông tin sản phẩm

                existingBlog.Title = blog.Title;

                existingBlog.Status = blog.Status;



                // Chỉ cập nhật UpdatedBy và UpdatedDate

                if (currentUser != null)

                {

                    existingBlog.UpdatedBy = currentUser.Username;

                    existingBlog.UpdatedDate = DateTime.Now;

                }



                // Xử lý upload ảnh đại diện

                if (mainImage != null && mainImage.Length > 0)

                {

                    // Xóa ảnh cũ nếu có

                    if (!string.IsNullOrEmpty(existingBlog.Img))

                    {

                        var oldMainPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingBlog.Img.TrimStart('/'));

                        if (System.IO.File.Exists(oldMainPath))

                        {

                            System.IO.File.Delete(oldMainPath);

                        }



                        // Xóa ảnh cũ trong ProductImages

                        var oldMainImage = await _context.ProductImages

                            .FirstOrDefaultAsync(pi => pi.ProductId == id && pi.IsMainImage);

                        if (oldMainImage != null)

                        {

                            _context.ProductImages.Remove(oldMainImage);

                        }

                    }



                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(mainImage.FileName);

                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/blogs", uniqueFileName);

                    var relativePath = "/images/blogs/" + uniqueFileName;



                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));



                    using (var stream = new FileStream(filePath, FileMode.Create))

                    {

                        await mainImage.CopyToAsync(stream);

                    }



                    existingBlog.Img = relativePath;



                    // Thêm ảnh mới vào ProductImages

                    var blogImage = new BlogImage

                    {

                        Blog_ID = id,

                        Img = relativePath,

                        IsMainImage = true,

                        CreatedAt = DateTime.Now

                    };

                    _context.BlogImages.Add(blogImage);

                }





                _context.Update(existingBlog);

                await _context.SaveChangesAsync();



                TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";

                return RedirectToAction(nameof(Index));

            }

            catch (Exception ex)

            {

                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật bài viết: " + ex.Message);

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

                .FirstOrDefaultAsync(m => m.Blog_ID == id);

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

            var blog = await _context.Blogs.FindAsync(id);

            if (blog != null)

            {

                _context.Blogs.Remove(blog);

            }



            try

            {

                // Xóa các ảnh liên quan

                var images = await _context.BlogImages.Where(pi => pi.Blog_ID == id).ToListAsync();

                foreach (var image in images)

                {

                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Img.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))

                    {

                        System.IO.File.Delete(filePath);

                    }

                    _context.BlogImages.Remove(image);

                }



                // Xóa sản phẩm

                _context.Blogs.Remove(blog);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa bài viết thành công";

            }

            catch (Exception ex)

            {

                TempData["Error"] = "Có lỗi xảy ra khi xóa bài viết: " + ex.Message;

            }



            return RedirectToAction(nameof(Index));

        }

        // Xóa ảnh sản phẩm

        [HttpPost]

        public async Task<IActionResult> DeleteImage(int imgId)

        {

            try

            {

                var image = await _context.BlogImages

                    .Include(pi => pi.Blog)

                    .FirstOrDefaultAsync(pi => pi.ImgId == imgId);



                if (image == null)

                {

                    return Json(new { success = false, message = "Không tìm thấy ảnh" });

                }



                // Không cho phép xóa ảnh chính

                if (image.IsMainImage)

                {

                    return Json(new { success = false, message = "Không thể xóa ảnh chính của bài viết" });

                }



                // Xóa file ảnh từ thư mục

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Img.TrimStart('/'));

                if (System.IO.File.Exists(filePath))

                {

                    System.IO.File.Delete(filePath);

                }



                // Xóa record trong database

                _context.BlogImages.Remove(image);

                await _context.SaveChangesAsync();



                return Json(new { success = true, message = "Xóa ảnh thành công" });

            }

            catch (Exception ex)

            {

                return Json(new { success = false, message = "Lỗi khi xóa ảnh: " + ex.Message });

            }

        }

        // Thêm action để quản lý ảnh bài viết

        [HttpGet]

        public async Task<IActionResult> ManageImages(int id)

        {

            var blog = await _context.Blogs

                .Include(p => p.BlogImages)

                .FirstOrDefaultAsync(p => p.Blog_ID == id);



            if (blog == null)

            {

                return NotFound();

            }



            return View(blog);

        }

        [HttpPost]

        public async Task<IActionResult> UploadImages(int Blog_ID, List<IFormFile> images)

        {

            try

            {

                var blog = await _context.Blogs.FindAsync(Blog_ID);

                if (blog == null)

                {

                    return Json(new { success = false, message = "Không tìm thấy bài viết" });

                }



                foreach (var image in images)

                {

                    if (image != null && image.Length > 0)

                    {

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

                        var filePath = Path.Combine("wwwroot/images/blogs", fileName);



                        using (var stream = new FileStream(filePath, FileMode.Create))

                        {

                            await image.CopyToAsync(stream);

                        }



                        var blogImage = new BlogImage

                        {

                            Blog_ID = Blog_ID,

                            Img = "/images/blogs/" + fileName,

                            CreatedAt = DateTime.Now

                        };



                        _context.BlogImages.Add(blogImage);

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

        public async Task<IActionResult> SetMainImage(int imgId)

        {

            try

            {

                var image = await _context.BlogImages

                    .Include(pi => pi.Blog)

                    .FirstOrDefaultAsync(pi => pi.ImgId == imgId);



                if (image == null)

                {

                    return Json(new { success = false, message = "Không tìm thấy ảnh" });

                }



                // Cập nhật ảnh chính cũ thành false

                var oldMainImage = await _context.BlogImages

                    .FirstOrDefaultAsync(pi => pi.Blog_ID == image.Blog_ID && pi.IsMainImage);

                if (oldMainImage != null)

                {

                    oldMainImage.IsMainImage = false;

                }



                // Đặt ảnh mới làm ảnh chính

                image.IsMainImage = true;

                image.Blog.Img = image.Img;



                await _context.SaveChangesAsync();



                return Json(new { success = true, message = "Đã đặt làm ảnh chính" });

            }

            catch (Exception ex)

            {

                return Json(new { success = false, message = "Lỗi khi đặt ảnh chính: " + ex.Message });

            }

        }



        private bool BlogExists(int id)

        {

            return _context.Blogs.Any(e => e.Blog_ID == id);

        }

    }

}


