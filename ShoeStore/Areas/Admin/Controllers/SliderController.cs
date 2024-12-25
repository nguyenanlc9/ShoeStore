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

        public SliderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Thêm phương thức helper để xử lý upload file

        private async Task<string> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "sliders");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/images/sliders/" + uniqueFileName;
        }

        // GET: Admin/Slider
        public async Task<IActionResult> Index()
        {
            return View(await _context.Slider.ToListAsync());
        }

        // GET: Admin/Slider/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Slider
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SliderDTO sliderDTO)
        {
            var userInfo = HttpContext.Session.Get<User>("userInfo");
            var userName = "";
            if (userInfo != null) userName = userInfo.Username;

            if (ModelState.IsValid)
            {
                // Tạo đối tượng Slider từ DTO
                var slider = new Slider
                {
                    Name = sliderDTO.Name,
                    Link = sliderDTO.Link,
                    Img = null, // Sẽ được cập nhật sau khi upload ảnh
                };

                // Kiểm tra và upload ảnh
                if (sliderDTO.Img != null)
                {
                    slider.Img = await UploadFile(sliderDTO.Img);
                }

                // Lưu slider vào database
                _context.Add(slider);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, trả lại view cùng dữ liệu sliderDTO
            return View(sliderDTO);
        }


        // GET: Admin/Slider/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Slider.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }
            return View(slider);
        }

        // POST: Admin/Slider/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Slider_ID,Name,Link,Img")] Slider slider)
        {
            if (id != slider.Slider_ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(slider);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SliderExists(slider.Slider_ID))
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
            return View(slider);
        }

        // GET: Admin/Slider/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slider = await _context.Slider
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
            var slider = await _context.Slider.FindAsync(id);
            if (slider != null)
            {
                _context.Slider.Remove(slider);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SliderExists(int id)
        {
            return _context.Slider.Any(e => e.Slider_ID == id);
        }
    }
}
