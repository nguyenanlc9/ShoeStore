using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ShoeStore.Utils
{
    public static class CookieExtensions
    {
        public static void Append<T>(this IResponseCookies cookies, string key, T value, CookieOptions opt)
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };
            cookies.Append(key, JsonSerializer.Serialize(value, options), opt);
        }

        public static T? Get<T>(this IRequestCookieCollection cookies, string key)
        {
            var value = cookies[key];
            if (value == null) return default;

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };
            return JsonSerializer.Deserialize<T>(value, options);
        }
    }
}
