public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string FromEmail { get; set; }
    public string SmtpPassword { get; set; }
    public bool EnableSsl { get; set; }
    public string DisplayName { get; set; }
} 