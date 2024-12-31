using System.ComponentModel.DataAnnotations;

public class ChangePasswordDTO
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; }

    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    public string ConfirmPassword { get; set; }
} 