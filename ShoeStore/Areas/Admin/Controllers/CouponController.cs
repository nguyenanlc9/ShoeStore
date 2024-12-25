using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Coupon
        public async Task<IActionResult> Index()
        {
            return View(await _context.Coupons.ToListAsync());
        }

        // GET: Admin/Coupon/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons.FirstOrDefaultAsync(m => m.CouponId == id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // GET: Admin/Coupon/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] Coupon request)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra logic ngày bắt đầu và ngày kết thúc
                if (request.DateStart > request.DateEnd)
                {
                    ModelState.AddModelError("DateEnd", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
                    return View(request);
                }

                var coupon = new Coupon
                {
                    CouponCode = request.CouponCode,
                    CouponName = request.CouponName,
                    DiscountPercentage = request.DiscountPercentage,
                    Description = request.Description,
                    DateStart = request.DateStart,
                    DateEnd = request.DateEnd,
                    Quantity = request.Quantity,
                    Status = request.Status
                };

                _context.Add(coupon);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(request);
        }

        // GET: Admin/Coupon/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        // POST: Admin/Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CouponId,CouponCode,CouponName,DiscountPercentage,Description,DateStart,DateEnd,Quantity,Status")] Coupon coupon)
        {
            if (id != coupon.CouponId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra logic ngày bắt đầu và ngày kết thúc
                if (coupon.DateStart > coupon.DateEnd)
                {
                    ModelState.AddModelError("DateEnd", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
                    return View(coupon);
                }

                try
                {
                    _context.Update(coupon);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật mã giảm giá thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.CouponId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(coupon);
        }

        // GET: Admin/Coupon/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons.FirstOrDefaultAsync(m => m.CouponId == id);
            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // POST: Admin/Coupon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mã giảm giá đã được xóa thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.CouponId == id);
        }
    }
}
