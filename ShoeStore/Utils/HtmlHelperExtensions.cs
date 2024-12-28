using System.Text.RegularExpressions;

namespace ShoeStore.Utils
{
    public static class HtmlHelperExtensions
    {
        public static string StripHtml(this string input, bool keepFormatting = false)
        {
            if (string.IsNullOrEmpty(input)) return input;

            if (keepFormatting)
            {
                // Thay thế các thẻ HTML bằng xuống dòng
                input = input.Replace("<br>", "\n")
                            .Replace("<br/>", "\n")
                            .Replace("<br />", "\n")
                            .Replace("</p><p>", "\n\n")  // Khoảng cách giữa các đoạn
                            .Replace("<p>", "")
                            .Replace("</p>", "\n")
                            .Replace("<strong>", "")
                            .Replace("</strong>", "");

                // Xử lý các thẻ khác nếu có
                input = Regex.Replace(input, "<.*?>", "");

                // Xử lý khoảng trắng và xuống dòng
                input = input.Replace("&nbsp;", " ");
                input = Regex.Replace(input, @" +", " ");  // Gộp nhiều khoảng trắng
                input = Regex.Replace(input, @"(\r\n|\r|\n){2,}", "\n\n");  // Gộp nhiều dòng trống
                input = input.Replace("\n ", "\n");  // Xóa khoảng trắng đầu dòng

                return input.Trim();
            }
            else
            {
                // Loại bỏ tất cả các thẻ HTML và giữ text
                return Regex.Replace(input, "<.*?>", " ").Trim();
            }
        }
    }
} 