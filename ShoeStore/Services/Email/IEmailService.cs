using System.Threading.Tasks;

namespace ShoeStore.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailConfirmationAsync(string email, string confirmationLink);
    }
} 