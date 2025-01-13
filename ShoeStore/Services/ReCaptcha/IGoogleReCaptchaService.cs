namespace ShoeStore.Services.ReCaptcha
{
    public interface IGoogleReCaptchaService
    {
        string LastError { get; }
        Task<bool> VerifyToken(string token);
    }
} 