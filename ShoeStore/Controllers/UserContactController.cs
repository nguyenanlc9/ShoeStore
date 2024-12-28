using Microsoft.AspNetCore.Mvc;
using ShoeStore.Models; // Thay đổi theo namespace của bạn
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
    public IActionResult Submit(ContactUser model)
    {
        if (ModelState.IsValid)
        {
            // Add the contact information to the database
            _context.ContactUsers.Add(model); // Add the new contact to the ContactUsers table
            _context.SaveChanges(); // Save the changes to the database

            TempData["SuccessMessage"] = "Gửi thông tin thành công!";
            return RedirectToAction("Contact", "Home"); // Redirect after success
        }
        else
        {
            TempData["ErrorMessage"] = "Đã có lỗi xảy ra. Vui lòng thử lại.";
            return RedirectToAction("Contact", "Home"); // Redirect with error message
        }
    }
}
