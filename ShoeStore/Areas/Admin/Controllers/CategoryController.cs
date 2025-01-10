using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;
using ShoeStore.Utils;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Category
        public async Task<IActionResult> Index()
        {
            ViewData["ActiveMenu"] = "category";
            return View(await _context.Categories
                .Include(c => c.Products)
                .ThenInclude(p => p.ProductSizeStocks)
                .ToListAsync());
        }

        // GET: Admin/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Admin/Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Category/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,Name")] Category category)
        {
            try
            {
                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");
                if (currentUser == null)
                {
                    ModelState.AddModelError("", "Phiên đăng nhập đã hết hạn");
                    return View(category);
                }

                category.CreatedBy = currentUser.Username;
                category.CreatedDate = DateTime.Now;
                category.UpdatedBy = currentUser.Username;
                category.UpdatedDate = DateTime.Now;

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm danh mục thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View(category);
            }
        }

        // GET: Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/Category/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name")] Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            try
            {
                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");
                var existingCategory = await _context.Categories.FindAsync(id);

                if (existingCategory == null)
                {
                    return NotFound();
                }

                existingCategory.Name = category.Name;
                existingCategory.UpdatedBy = currentUser.Username;
                existingCategory.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật danh mục thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
            }

            return View(category);
        }

        // GET: Admin/Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Admin/Category/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .ThenInclude(p => p.ProductSizeStocks)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục" });
            }

            // Kiểm tra xem có sản phẩm nào thuộc danh mục này không
            if (category.Products.Any())
            {
                // Kiểm tra xem có sản phẩm nào còn tồn kho không
                var hasStock = category.Products.Any(p => p.ProductSizeStocks.Sum(pss => pss.StockQuantity) > 0);
                if (hasStock)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục vì còn sản phẩm tồn kho" });
                }
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}