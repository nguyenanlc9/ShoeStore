using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Filters;
using ShoeStore.Models;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class MemberRankController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MemberRankController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/MemberRank
        public async Task<IActionResult> Index()
        {
            return View(await _context.MemberRanks.ToListAsync());
        }

        // GET: Admin/MemberRank/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var memberRank = await _context.MemberRanks
                .FirstOrDefaultAsync(m => m.RankId == id);
            if (memberRank == null)
            {
                return NotFound();
            }

            return View(memberRank);
        }

        // GET: Admin/MemberRank/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/MemberRank/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RankId,RankName,MinimumSpent,DiscountPercent,Description,BadgeImage")] MemberRank memberRank)
        {
            if (ModelState.IsValid)
            {
                _context.Add(memberRank);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(memberRank);
        }

        // GET: Admin/MemberRank/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var memberRank = await _context.MemberRanks.FindAsync(id);
            if (memberRank == null)
            {
                return NotFound();
            }
            return View(memberRank);
        }

        // POST: Admin/MemberRank/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RankId,RankName,MinimumSpent,DiscountPercent,Description,BadgeImage")] MemberRank memberRank)
        {
            if (id != memberRank.RankId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(memberRank);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MemberRankExists(memberRank.RankId))
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
            return View(memberRank);
        }

        // GET: Admin/MemberRank/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var memberRank = await _context.MemberRanks
                .FirstOrDefaultAsync(m => m.RankId == id);
            if (memberRank == null)
            {
                return NotFound();
            }

            return View(memberRank);
        }

        // POST: Admin/MemberRank/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var memberRank = await _context.MemberRanks.FindAsync(id);
            if (memberRank != null)
            {
                _context.MemberRanks.Remove(memberRank);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MemberRankExists(int id)
        {
            return _context.MemberRanks.Any(e => e.RankId == id);
        }
    }
}
