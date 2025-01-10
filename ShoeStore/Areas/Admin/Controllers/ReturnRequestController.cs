using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Services.Email;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReturnRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ReturnRequestController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Admin/ReturnRequest
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ReturnRequests.Include(r => r.Order).Include(r => r.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admin/ReturnRequest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var returnRequest = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReturnId == id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            return View(returnRequest);
        }

        // GET: Admin/ReturnRequest/Create
        public IActionResult Create()
        {
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "PhoneNumber");
            ViewData["UserId"] = new SelectList(_context.Users, "UserID", "Email");
            return View();
        }

        // POST: Admin/ReturnRequest/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReturnId,OrderId,UserId,RequestDate,Reason,Images,Status,AdminNote")] ReturnRequest returnRequest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(returnRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "PhoneNumber", returnRequest.OrderId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserID", "Email", returnRequest.UserId);
            return View(returnRequest);
        }

        // GET: Admin/ReturnRequest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "PhoneNumber", returnRequest.OrderId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserID", "Email", returnRequest.UserId);
            return View(returnRequest);
        }

        // POST: Admin/ReturnRequest/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReturnId,OrderId,UserId,RequestDate,Reason,Images,Status,AdminNote")] ReturnRequest returnRequest)
        {
            if (id != returnRequest.ReturnId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(returnRequest);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật yêu cầu đổi trả thành công";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReturnRequestExists(returnRequest.ReturnId))
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
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "PhoneNumber", returnRequest.OrderId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserID", "Email", returnRequest.UserId);
            return View(returnRequest);
        }

        // GET: Admin/ReturnRequest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var returnRequest = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReturnId == id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            return View(returnRequest);
        }

        // POST: Admin/ReturnRequest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var returnRequest = await _context.ReturnRequests.FindAsync(id);
            if (returnRequest != null)
            {
                _context.ReturnRequests.Remove(returnRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReturnRequestExists(int id)
        {
            return _context.ReturnRequests.Any(e => e.ReturnId == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReturnStatus status, string note)
        {
            try
            {
                var returnRequest = await _context.ReturnRequests
                    .Include(r => r.User)
                    .Include(r => r.Order)
                    .FirstOrDefaultAsync(r => r.ReturnId == id);

                if (returnRequest == null)
                {
                    TempData["Error"] = "Không tìm thấy yêu cầu đổi trả";
                    return RedirectToAction(nameof(Index));
                }

                var oldStatus = returnRequest.Status;
                returnRequest.Status = status;
                returnRequest.AdminNote = note;

                _context.Update(returnRequest);
                await _context.SaveChangesAsync();

                // Gửi email thông báo cho khách hàng
                string statusText = status switch
                {
                    ReturnStatus.Approved => "đã được duyệt",
                    ReturnStatus.Completed => "đã hoàn thành",
                    ReturnStatus.Rejected => "đã bị từ chối",
                    _ => "đã được cập nhật"
                };

                string emailSubject = $"Cập nhật trạng thái yêu cầu đổi trả #{returnRequest.ReturnId}";
                string emailBody = $@"
                    <h2>Xin chào {returnRequest.User.FullName},</h2>
                    <p>Yêu cầu đổi trả cho đơn hàng <strong>{returnRequest.Order.OrderCode}</strong> của bạn {statusText}.</p>
                    {(status == ReturnStatus.Rejected ? $"<p>Lý do: {note}</p>" : "")}
                    <p>Vui lòng đăng nhập vào tài khoản của bạn để xem chi tiết.</p>
                    <p>Trân trọng,<br>ShoeStore</p>";

                await _emailService.SendEmailAsync(returnRequest.User.Email, emailSubject, emailBody);

                TempData["Success"] = $"Cập nhật trạng thái thành công thành {statusText}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật trạng thái: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
