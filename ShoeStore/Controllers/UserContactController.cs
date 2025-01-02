using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models;
using System.Threading.Tasks;

public class UserContactController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserContactController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Submit(ContactUser model)
    {
        if (ModelState.IsValid)
        {
            // Add the contact information to the database
            _context.ContactUsers.Add(model);
            await _context.SaveChangesAsync();

            // Create notification for admin
            var contact = new Contact
            {
                ContactName = $"{model.ContactUFirstName} {model.ContactULastName}",
                ContactEmail = model.ContactUEmail,
                ContactPhone = model.ContactUPhone,
                ContactDescription = model.ContactUMessage
            };

            TempData["SuccessMessage"] = "Gửi thông tin thành công!";
            return RedirectToAction("Contact", "Home");
        }
        else
        {
            TempData["ErrorMessage"] = "Đã có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction("Contact", "Home");
        }
    }
}
