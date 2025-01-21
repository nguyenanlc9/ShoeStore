using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Models.DTO.Requset;
using ShoeStore.Filters;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AboutController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
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

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "about");
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
                return null;
            }
        }

        private void DeleteImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "about", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // GET: Admin/About
        public async Task<IActionResult> Index()
        {
            return View(await _context.Abouts.OrderByDescending(a => a.CreatedAt).ToListAsync());
        }

        // GET: Admin/About/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var about = await _context.Abouts
                .FirstOrDefaultAsync(m => m.AboutId == id);
            if (about == null)
            {
                return NotFound();
            }

            return View(about);
        }

        // GET: Admin/About/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/About/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AboutId,Title,Content,ImagePath,Status")] About about)
        {
            if (ModelState.IsValid)
            {
                about.CreatedAt = DateTime.Now;
                about.Status = 1; // Set default status to 1 (Hiển thị)
                _context.Add(about);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(about);
        }

        // GET: Admin/About/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var about = await _context.Abouts.FindAsync(id);
            if (about == null)
            {
                return NotFound();
            }

            var aboutDTO = new AboutDTO
            {
                AboutId = about.AboutId,
                Title = about.Title,
                Content = about.Content,
                ImagePath = about.ImagePath,
                Status = about.Status
            };

            return View(aboutDTO);
        }

        // POST: Admin/About/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AboutId,Title,Content,ImagePath,Status,CreatedAt")] About about)
        {
            if (id != about.AboutId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    about.UpdatedAt = DateTime.Now;
                    _context.Update(about);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AboutExists(about.AboutId))
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
            return View(about);
        }

        // GET: Admin/About/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var about = await _context.Abouts
                .FirstOrDefaultAsync(m => m.AboutId == id);
            if (about == null)
            {
                return NotFound();
            }

            return View(about);
        }

        // POST: Admin/About/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var about = await _context.Abouts.FindAsync(id);
            if (about != null)
            {
                DeleteImage(about.ImagePath);
                _context.Abouts.Remove(about);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa trang giới thiệu thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AboutExists(int id)
        {
            return _context.Abouts.Any(e => e.AboutId == id);
        }
    }
} 