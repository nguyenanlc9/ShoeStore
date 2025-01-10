using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.DTO.Request
{
    public class UserLoginDTO
    {
        [Required(ErrorMessage = "Username không được để trống")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
} 