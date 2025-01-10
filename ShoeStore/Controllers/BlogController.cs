using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using X.PagedList;

namespace ShoeStore.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BlogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Blog
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 6; // Số bài viết trên mỗi trang
            int pageNumber = page ?? 1;

            var blogs = await _context.Blogs
                .Where(b => b.IsVisible)
                .OrderByDescending(b => b.CreatedDate)
                .ToPagedListAsync(pageNumber, pageSize);

            return View(blogs);
        }

        // GET: Blog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Include(b => b.BlogImages)
                .FirstOrDefaultAsync(m => m.BlogId == id && m.IsVisible);

            if (blog == null)
            {
                return NotFound();
            }

            // Lấy các bài viết liên quan (cùng tác giả hoặc mới nhất)
            var relatedBlogs = await _context.Blogs
                .Where(b => b.BlogId != id && b.IsVisible)
                .OrderByDescending(b => b.CreatedDate)
                .Take(3)
                .ToListAsync();

            ViewBag.RelatedBlogs = relatedBlogs;

            return View(blog);
        }
    }
} 