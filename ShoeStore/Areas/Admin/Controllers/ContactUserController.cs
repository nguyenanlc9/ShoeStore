using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Services.Email;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ContactUserController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Admin/ContactUser/Feedback/5
        [HttpGet]
        [Route("Admin/ContactUser/Feedback/{id}")]
        public async Task<IActionResult> Feedback(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactUser = await _context.ContactUsers
                .FirstOrDefaultAsync(m => m.ContactUId == id);
            if (contactUser == null)
            {
                return NotFound();
            }

            return View(contactUser);
        }

        // POST: Admin/ContactUser/SendFeedback
        [HttpPost]
        [Route("Admin/ContactUser/SendFeedback")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendFeedback(int id, string replyMessage)
        {
            var contactUser = await _context.ContactUsers.FindAsync(id);
            if (contactUser == null)
            {
                return NotFound();
            }

            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                using (var client = new SmtpClient("smtp.gmail.com"))
                {
                    client.Port = 587;
                    client.Credentials = new NetworkCredential(
                        emailSettings["FromEmail"],
                        emailSettings["SmtpPassword"]
                    );
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress(emailSettings["FromEmail"]);
                    mailMessage.To.Add(contactUser.ContactUEmail);
                    mailMessage.Subject = "Phản hồi từ ShoeStore";
                    mailMessage.Body = replyMessage;
                    mailMessage.IsBodyHtml = true;

                    await client.SendMailAsync(mailMessage);
                }

                TempData["SuccessMessage"] = "Phản hồi đã được gửi thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi gửi email: " + ex.Message;
                return View("Feedback", contactUser);
            }
        }
    // GET: Admin/ContactUser
    public async Task<IActionResult> Index()
        {
            return View(await _context.ContactUsers.ToListAsync());
        }

        // GET: Admin/ContactUser/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactUser = await _context.ContactUsers
                .FirstOrDefaultAsync(m => m.ContactUId == id);
            if (contactUser == null)
            {
                return NotFound();
            }

            return View(contactUser);
        }

        // GET: Admin/ContactUser/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ContactUser/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContactUId,ContactUFirstName,ContactULastName,ContactUEmail,ContactUAddress,ContactUPhone,ContactUMessage")] ContactUser contactUser)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contactUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contactUser);
        }

        // GET: Admin/ContactUser/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactUser = await _context.ContactUsers.FindAsync(id);
            if (contactUser == null)
            {
                return NotFound();
            }
            return View(contactUser);
        }

        // POST: Admin/ContactUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContactUId,ContactUFirstName,ContactULastName,ContactUEmail,ContactUAddress,ContactUPhone,ContactUMessage")] ContactUser contactUser)
        {
            if (id != contactUser.ContactUId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contactUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactUserExists(contactUser.ContactUId))
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
            return View(contactUser);
        }

        // GET: Admin/ContactUser/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactUser = await _context.ContactUsers
                .FirstOrDefaultAsync(m => m.ContactUId == id);
            if (contactUser == null)
            {
                return NotFound();
            }

            return View(contactUser);
        }

        // POST: Admin/ContactUser/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contactUser = await _context.ContactUsers.FindAsync(id);
            if (contactUser != null)
            {
                _context.ContactUsers.Remove(contactUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactUserExists(int id)
        {
            return _context.ContactUsers.Any(e => e.ContactUId == id);
        }
    }
}
