using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
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

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:FromEmail"]));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:SmtpPort"]),
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:Username"],
                _configuration["EmailSettings:SmtpPassword"]
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationLink)
        {
            string subject = "Xác nhận email đăng ký tài khoản";
            string body = $@"
                <h2>Xác nhận email đăng ký tài khoản</h2>
                <p>Cảm ơn bạn đã đăng ký tài khoản. Để hoàn tất quá trình đăng ký, vui lòng click vào link bên dưới để xác nhận email của bạn:</p>
                <p><a href='{confirmationLink}'>Xác nhận email</a></p>
                <p>Link xác nhận này sẽ hết hạn sau 24 giờ.</p>
                <p>Nếu bạn không thực hiện đăng ký tài khoản, vui lòng bỏ qua email này.</p>
                <p>Trân trọng,<br>ShoeStore Team</p>";

            await SendEmailAsync(email, subject, body);
        }
    }
} 