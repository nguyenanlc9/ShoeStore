using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly EmailSettings _emailSettings;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _emailSettings = _configuration.GetSection("EmailSettings").Get<EmailSettings>();
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.DisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using (var client = new SmtpClient(_emailSettings.SmtpServer))
            {
                client.Port = _emailSettings.SmtpPort;
                client.Credentials = new NetworkCredential(_emailSettings.FromEmail, _emailSettings.SmtpPassword);
                client.EnableSsl = _emailSettings.EnableSsl;

                await client.SendMailAsync(message);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi gửi email: {ex.Message}");
        }
    }
} 