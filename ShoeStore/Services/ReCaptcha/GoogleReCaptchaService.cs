using System.Text.Json;
using Microsoft.Extensions.Options;
using ShoeStore.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace ShoeStore.Services.ReCaptcha
{
    public class GoogleReCaptchaService : IGoogleReCaptchaService
    {
        private readonly IOptions<GoogleReCaptchaConfig> _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleReCaptchaService> _logger;
        public string LastError { get; private set; }

        public GoogleReCaptchaService(
            IOptions<GoogleReCaptchaConfig> config, 
            HttpClient httpClient,
            ILogger<GoogleReCaptchaService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> VerifyToken(string token)
        {
            try
            {
                LastError = null;
                if (string.IsNullOrEmpty(token))
                {
                    LastError = "Token reCAPTCHA trống";
                    _logger.LogWarning(LastError);
                    return false;
                }

                var url = $"https://www.google.com/recaptcha/api/siteverify?secret={_config.Value.SecretKey}&response={token}";
                var response = await _httpClient.PostAsync(url, null);
                
                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"Lỗi kết nối: {response.StatusCode}";
                    _logger.LogError(LastError);
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response from Google: {responseString}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseData = JsonSerializer.Deserialize<ReCaptchaResponse>(responseString, options);
                
                if (responseData == null)
                {
                    LastError = "Không thể đọc response từ Google";
                    _logger.LogError(LastError);
                    return false;
                }

                if (!responseData.Success && responseData.ErrorCodes != null && responseData.ErrorCodes.Length > 0)
                {
                    LastError = GetErrorMessage(responseData.ErrorCodes);
                    _logger.LogWarning(LastError);
                }

                return responseData.Success;
            }
            catch (Exception ex)
            {
                LastError = $"Lỗi xác thực reCAPTCHA: {ex.Message}";
                _logger.LogError(ex, LastError);
                return false;
            }
        }

        private string GetErrorMessage(string[] errorCodes)
        {
            var errorMessages = new Dictionary<string, string>
            {
                {"missing-input-secret", "Thiếu secret key"},
                {"invalid-input-secret", "Secret key không hợp lệ"},
                {"missing-input-response", "Thiếu token response"},
                {"invalid-input-response", "Token response không hợp lệ"},
                {"bad-request", "Request không hợp lệ"},
                {"timeout-or-duplicate", "Token đã hết hạn hoặc đã được sử dụng"}
            };

            var errors = errorCodes.Select(code => errorMessages.ContainsKey(code) ? errorMessages[code] : code);
            return string.Join(", ", errors);
        }

        private class ReCaptchaResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("challenge_ts")]
            public string ChallengeTs { get; set; }

            [JsonPropertyName("hostname")]
            public string Hostname { get; set; }

            [JsonPropertyName("error-codes")]
            public string[] ErrorCodes { get; set; }
        }
    }
} 