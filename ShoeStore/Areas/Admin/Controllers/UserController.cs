using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using System.Security.Cryptography;
using System.Text;
using ShoeStore.Utils;
using ShoeStore.Filters;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
    }

        // GET: Admin/User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .ToListAsync();
            return View(users);
        }

        // GET: Admin/User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Admin/User/Create
        public IActionResult Create()
        {
            ViewData["RoleID"] = new SelectList(_context.Roles, "RoleID", "RoleName");
            return View();
        }

        // POST: Admin/User/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,PasswordHash,FullName,Email,Phone,Address,RoleID")] User user)
        {
            try 
            {
                // Kiểm tra ModelState và in ra lỗi để debug
                if (!ModelState.IsValid)
                {
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            // Log hoặc in ra lỗi
                            System.Diagnostics.Debug.WriteLine(error.ErrorMessage);
                        }
                    }
                }

                // Kiểm tra tên đăng nhập đã tồn tại
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == user.Username);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                    ViewData["RoleID"] = new SelectList(_context.Roles, "RoleID", "RoleName", user.RoleID);
                    return View(user);
                }

                // Thiết lập các giá trị mặc định
                user.RegisterDate = DateTime.Now;
                user.CreatedDate = DateTime.Now;
                user.Status = true;
                user.LastLogin = null;

                // Mã hóa mật khẩu
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    user.PasswordHash = PasswordHelper.HashPassword(user.PasswordHash);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine(ex.Message);
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);
            }

            ViewData["RoleID"] = new SelectList(_context.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // GET: Admin/User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["RoleID"] = new SelectList(_context.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // POST: Admin/User/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserID,Username,PasswordHash,NewPassword,FullName,Email,Phone,Address,Status,RegisterDate,LastLogin,CreatedDate,RoleID")] User user)
        {
            if (id != user.UserID)
            {
                return NotFound();
            }

            try
            {
                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserID == id);

                if (existingUser == null)
                {
                    return NotFound();
                }

                // Xử lý mật khẩu
                if (!string.IsNullOrEmpty(user.NewPassword))
                {
                    // Nếu có nhập mật khẩu mới thì mã hóa và cập nhật
                    user.PasswordHash = PasswordHelper.HashPassword(user.NewPassword);
                }
                // Nếu không nhập mật khẩu mới thì giữ nguyên mật khẩu cũ
                
                user.RegisterDate = existingUser.RegisterDate;
                user.CreatedDate = existingUser.CreatedDate;
                user.LastLogin = existingUser.LastLogin;

                _context.Entry(existingUser).State = EntityState.Detached;
                _context.Update(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật dữ liệu: " + ex.Message);
            }

            ViewData["RoleID"] = new SelectList(_context.Roles, "RoleID", "RoleName", user.RoleID);
            return View(user);
        }

        // GET: Admin/User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }
    }
}
