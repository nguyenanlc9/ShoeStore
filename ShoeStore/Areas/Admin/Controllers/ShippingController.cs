using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;
using ShoeStore.Services.APIAddress;
using ShoeStore.Services.Order;

namespace ShoeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ShippingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAddressService _addressService;
        private readonly IShippingService _shippingService;

        public ShippingController(
            ApplicationDbContext context,
            IAddressService addressService,
            IShippingService shippingService)
        {
            _context = context;
            _addressService = addressService;
            _shippingService = shippingService;
        }

        public async Task<IActionResult> Index()
        {
            var rates = await _context.ShippingRates
                .OrderBy(r => r.Province)
                .ToListAsync();
            return View(rates);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Provinces = await _addressService.GetProvinces();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShippingRate model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await _addressService.GetProvinces();
                return View(model);
            }

            try
            {
                // Kiểm tra tỉnh/thành phố có tồn tại trong API không
                var provinces = await _addressService.GetProvinces();
                var provinceExists = provinces.Any(p => p.Name.ToLower() == model.Province.ToLower());

                if (!provinceExists)
                {
                    ModelState.AddModelError("Province", "Tỉnh/thành phố không hợp lệ");
                    ViewBag.Provinces = provinces;
                    return View(model);
                }

                // Kiểm tra tỉnh/thành phố đã tồn tại trong database chưa
                var existingProvince = await _context.ShippingRates
                    .Where(r => r.Province.ToLower() == model.Province.ToLower())
                    .AnyAsync();

                if (existingProvince)
                {
                    ModelState.AddModelError("Province", "Tỉnh/thành phố này đã có phí vận chuyển");
                    ViewBag.Provinces = provinces;
                    return View(model);
                }

                model.CreatedAt = DateTime.Now;
                _context.ShippingRates.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã thêm phí vận chuyển thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                ViewBag.Provinces = await _addressService.GetProvinces();
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var rate = await _context.ShippingRates.FindAsync(id);
            if (rate == null)
            {
                return NotFound();
            }

            ViewBag.Provinces = await _addressService.GetProvinces();
            return View(rate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShippingRate model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await _addressService.GetProvinces();
                return View(model);
            }

            try
            {
                var existingRate = await _context.ShippingRates.FindAsync(model.Id);
                if (existingRate == null)
                {
                    return NotFound();
                }

                // Kiểm tra tỉnh/thành phố có tồn tại trong API không
                var provinces = await _addressService.GetProvinces();
                var provinceExists = provinces.Any(p => p.Name.ToLower() == model.Province.ToLower());

                if (!provinceExists)
                {
                    ModelState.AddModelError("Province", "Tỉnh/thành phố không hợp lệ");
                    ViewBag.Provinces = provinces;
                    return View(model);
                }

                // Kiểm tra nếu đổi tên tỉnh/thành phố thì không được trùng với tỉnh/thành phố khác
                if (existingRate.Province.ToLower() != model.Province.ToLower())
                {
                    var duplicateProvince = await _context.ShippingRates
                        .Where(r => r.Province.ToLower() == model.Province.ToLower())
                        .AnyAsync();

                    if (duplicateProvince)
                    {
                        ModelState.AddModelError("Province", "Tỉnh/thành phố này đã có phí vận chuyển");
                        ViewBag.Provinces = provinces;
                        return View(model);
                    }
                }

                existingRate.Province = model.Province;
                existingRate.BaseFee = model.BaseFee;
                existingRate.DeliveryDays = model.DeliveryDays;
                existingRate.IsActive = model.IsActive;
                existingRate.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật phí vận chuyển thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                ViewBag.Provinces = await _addressService.GetProvinces();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var rate = await _context.ShippingRates.FindAsync(id);
                if (rate == null)
                {
                    TempData["Error"] = "Không tìm thấy bản ghi";
                    return RedirectToAction(nameof(Index));
                }

                rate.IsActive = !rate.IsActive;
                rate.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Đã cập nhật trạng thái";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 