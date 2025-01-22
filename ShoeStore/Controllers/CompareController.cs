using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ShoeStore.Controllers
{
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompareController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var compareProducts = new List<CompareProduct>();
            var compareList = HttpContext.Session.Get<List<int>>("CompareList") ?? new List<int>();

            foreach (var productId in compareList)
            {
                var product = await _context.Products
                    .Include(p => p.Brands)
                    .Include(p => p.Categories)
                    .Include(p => p.ProductSizeStocks)
                        .ThenInclude(pss => pss.Size)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product != null)
                {
                    compareProducts.Add(new CompareProduct
                    {
                        ProductId = productId,
                        Product = product,
                        AddedDate = DateTime.Now
                    });
                }
            }

            return View(compareProducts);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCompare(int productId)
        {
            var compareList = HttpContext.Session.Get<List<int>>("CompareList") ?? new List<int>();

            if (compareList.Count >= 4)
            {
                return Json(new { success = false, message = "Bạn chỉ có thể so sánh tối đa 4 sản phẩm" });
            }

            if (compareList.Contains(productId))
            {
                return Json(new { success = false, message = "Sản phẩm này đã có trong danh sách so sánh" });
            }

            compareList.Add(productId);
            HttpContext.Session.Set("CompareList", compareList);

            return Json(new { success = true, message = "Đã thêm sản phẩm vào danh sách so sánh" });
        }

        [HttpPost]
        public IActionResult RemoveFromCompare(int productId)
        {
            var compareList = HttpContext.Session.Get<List<int>>("CompareList") ?? new List<int>();
            compareList.Remove(productId);
            HttpContext.Session.Set("CompareList", compareList);

            return Json(new { success = true, message = "Đã xóa sản phẩm khỏi danh sách so sánh" });
        }

        [HttpPost]
        public IActionResult ClearCompare()
        {
            HttpContext.Session.Remove("CompareList");
            return Json(new { success = true, message = "Đã xóa tất cả sản phẩm khỏi danh sách so sánh" });
        }
    }
}