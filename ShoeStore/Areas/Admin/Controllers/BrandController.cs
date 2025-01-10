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
    public class BrandController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BrandController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Brand
        public async Task<IActionResult> Index()
        {
            ViewData["ActiveMenu"] = "brand";
            return View(await _context.Brands.ToListAsync());
        }

        // GET: Admin/Brand/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(m => m.BrandId == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // GET: Admin/Brand/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Brand/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BrandId,Name")] Brand brand)
        {
            try
            {
                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");
                if (currentUser == null)
                {
                    ModelState.AddModelError("", "Phiên đăng nhập đã hết hạn");
                    return View(brand);
                }

                brand.CreatedBy = currentUser.Username;
                brand.CreatedDate = DateTime.Now;
                brand.UpdatedBy = currentUser.Username;
                brand.UpdatedDate = DateTime.Now;

                _context.Add(brand);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm thương hiệu thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                return View(brand);
            }
        }

        // GET: Admin/Brand/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Admin/Brand/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BrandId,Name")] Brand brand)
        {
            if (id != brand.BrandId)
            {
                return NotFound();
            }

            try
            {
                var currentUser = HttpContext.Session.Get<User>("AdminUserInfo");
                var existingBrand = await _context.Brands.FindAsync(id);

                if (existingBrand == null)
                {
                    return NotFound();
                }

                existingBrand.Name = brand.Name;
                existingBrand.UpdatedBy = currentUser.Username;
                existingBrand.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thương hiệu thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
            }

            return View(brand);
        }

        // GET: Admin/Brand/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .FirstOrDefaultAsync(m => m.BrandId == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Admin/Brand/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thương hiệu" });
            }

            try
            {
                // Kiểm tra xem có sản phẩm nào thuộc thương hiệu này không
                var products = await _context.Products.Where(p => p.BrandId == id).ToListAsync();
                if (products.Any())
                {
                    return Json(new { success = false, message = "Không thể xóa vì còn sản phẩm thuộc thương hiệu này" });
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.BrandId == id);
        }
    }
}