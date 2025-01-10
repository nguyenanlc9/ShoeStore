using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Utils;
using ShoeStore.Models.DTO.Requset;
using ShoeStore.Filters;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class SliderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SliderController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task<string> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return null;

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "sliders");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return uniqueFileName;
            }
            catch (Exception ex)
            {
                // Log error
                return null;
            }
        }

        private void DeleteImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "sliders", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // GET: Admin/Slider
        public async Task<IActionResult> Index()
        {
            return View(await _context.Sliders.OrderBy(s => s.Sort).ToListAsync());
        }

        // GET: Admin/Slider/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Sliders
                .FirstOrDefaultAsync(m => m.Slider_ID == id);
            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        // GET: Admin/Slider/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Slider/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SliderDTO sliderDTO)
        {
            try 
            {
                // Kiểm tra ModelState errors
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                                .Select(e => e.ErrorMessage);
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return View(sliderDTO);
                }

                // Kiểm tra file ảnh
                if (sliderDTO.ImageFile == null || sliderDTO.ImageFile.Length == 0)
                {
                    ModelState.AddModelError("ImageFile", "Vui lòng chọn hình ảnh");
                    return View(sliderDTO);
                }

                // Upload ảnh và lưu tên file
                string fileName = await UploadImage(sliderDTO.ImageFile);
                if (string.IsNullOrEmpty(fileName))
                {
                    ModelState.AddModelError("ImageFile", "Không thể upload hình ảnh");
                    return View(sliderDTO);
                }

                // Tạo đối tượng Slider từ DTO
                var slider = new Slider
                {
                    Name = sliderDTO.Name,
                    Title = sliderDTO.Title,
                    Description = sliderDTO.Description ?? "",  // Đảm bảo không null
                    Link = sliderDTO.Link ?? "",               // Đảm bảo không null
                    Status = sliderDTO.Status,
                    Sort = sliderDTO.Sort,
                    Img = fileName
                };

                // Đảm bảo Sort không null
                if (slider.Sort == 0)
                {
                    var maxSort = await _context.Sliders.MaxAsync(s => (int?)s.Sort) ?? 0;
                    slider.Sort = maxSort + 1;
                }

                _context.Add(slider);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Thêm slider thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log lỗi để debug
                System.Diagnostics.Debug.WriteLine($"Error in Create: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View(sliderDTO);
            }
        }

        // GET: Admin/Slider/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }

            // Chuyển đổi từ Slider sang SliderDTO
            var sliderDTO = new SliderDTO
            {
                Slider_ID = slider.Slider_ID,
                Name = slider.Name,
                Title = slider.Title,
                Description = slider.Description,
                Link = slider.Link,
                Status = slider.Status,
                Sort = slider.Sort,
                ImgPath = slider.Img
            };

            return View(sliderDTO);
        }

        // POST: Admin/Slider/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SliderDTO sliderDTO)
        {
            if (id != sliderDTO.Slider_ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var slider = await _context.Sliders.FindAsync(id);
                    if (slider == null)
                    {
                        return NotFound();
                    }

                    // Cập nhật thông tin từ DTO
                    slider.Name = sliderDTO.Name;
                    slider.Title = sliderDTO.Title;
                    slider.Description = sliderDTO.Description;
                    slider.Link = sliderDTO.Link;
                    slider.Status = sliderDTO.Status;
                    slider.Sort = sliderDTO.Sort;

                    // Xử lý upload ảnh mới nếu có
                    if (sliderDTO.ImageFile != null)
                    {
                        // Xóa ảnh cũ
                        DeleteImage(slider.Img);
                        // Upload ảnh mới
                        string newImagePath = await UploadImage(sliderDTO.ImageFile);
                        if (!string.IsNullOrEmpty(newImagePath))
                        {
                            slider.Img = newImagePath;
                        }
                    }

                    _context.Update(slider);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SliderExists(sliderDTO.Slider_ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(sliderDTO);
        }

        // GET: Admin/Slider/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Sliders
                .FirstOrDefaultAsync(m => m.Slider_ID == id);
            if (slider == null)
            {
                return NotFound();
            }

            return View(slider);
        }

        // POST: Admin/Slider/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var slider = await _context.Sliders.FindAsync(id);
            if (slider != null)
            {
                // Xóa file ảnh
                DeleteImage(slider.Img);
                // Xóa record trong database
                _context.Sliders.Remove(slider);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SliderExists(int id)
        {
            return _context.Sliders.Any(e => e.Slider_ID == id);
        }
    }
}
