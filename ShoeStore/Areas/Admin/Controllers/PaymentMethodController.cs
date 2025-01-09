using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class PaymentMethodController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentMethodController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var methods = await _context.PaymentMethodConfigs.ToListAsync();
            return View(methods);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentMethodConfig model)
        {
            if (ModelState.IsValid)
            {
                model.LastUpdated = DateTime.Now;
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["success"] = "Phương thức thanh toán đã được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var method = await _context.PaymentMethodConfigs.FindAsync(id);
            if (method == null)
            {
                return NotFound();
            }
            return View(method);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentMethodConfig model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    model.LastUpdated = DateTime.Now;
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Phương thức thanh toán đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentMethodExists(model.Id))
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
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, PaymentMethodStatus status)
        {
            var method = await _context.PaymentMethodConfigs.FindAsync(id);
            if (method == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phương thức thanh toán" });
            }

            method.Status = status;
            method.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
        }

        private bool PaymentMethodExists(int id)
        {
            return _context.PaymentMethodConfigs.Any(e => e.Id == id);
        }
    }
} 