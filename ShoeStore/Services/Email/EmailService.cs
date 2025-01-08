using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ShoeStore.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            using (var client = new SmtpClient())
            {
                var credential = new NetworkCredential
                {
                    UserName = _configuration["EmailSettings:FromEmail"],
                    Password = _configuration["EmailSettings:SmtpPassword"]
                };

                client.Credentials = credential;
                client.Host = _configuration["EmailSettings:SmtpServer"];
                client.Port = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                client.EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);

                using (var emailMessage = new MailMessage())
                {
                    emailMessage.To.Add(new MailAddress(email));
                    emailMessage.From = new MailAddress(_configuration["EmailSettings:FromEmail"], _configuration["EmailSettings:DisplayName"]);
                    emailMessage.Subject = subject;
                    emailMessage.Body = message;
                    emailMessage.IsBodyHtml = true;

                    await client.SendMailAsync(emailMessage);
                }
            }
        }

    }
} 