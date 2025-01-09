

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FooterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FooterController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Footer
        public async Task<IActionResult> Index()
        {
            var footers = await _context.Footers.ToListAsync();
            return View(footers);
        }

        // GET: Admin/Footer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var footer = await _context.Footers
                .FirstOrDefaultAsync(m => m.FooterId == id);
            if (footer == null)
            {
                return NotFound();
            }

            return View(footer);
        }

        // GET: Admin/Footer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Footer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FooterId,FooterAddress,FooterPhone,FooterEmail")] Footer footer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(footer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(footer);
        }

        // GET: Admin/Footer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var footer = await _context.Footers.FindAsync(id);
            if (footer == null)
            {
                return NotFound();
            }
            return View(footer);
        }

        // POST: Admin/Footer/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FooterId,FooterAddress,FooterPhone,FooterEmail")] Footer footer)
        {
            if (id != footer.FooterId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(footer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FooterExists(footer.FooterId))
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
            return View(footer);
        }

        // GET: Admin/Footer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var footer = await _context.Footers
                .FirstOrDefaultAsync(m => m.FooterId == id);
            if (footer == null)
            {
                return NotFound();
            }

            return View(footer);
        }

        // POST: Admin/Footer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var footer = await _context.Footers.FindAsync(id);
            if (footer != null)
            {
                _context.Footers.Remove(footer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FooterExists(int id)
        {
            return _context.Footers.Any(e => e.FooterId == id);
        }
    }
}
