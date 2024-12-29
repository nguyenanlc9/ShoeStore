using System.Threading.Tasks;

namespace ShoeStore.Services.Email
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body);
    }
}