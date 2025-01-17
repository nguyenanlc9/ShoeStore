using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ShoeStore.Models.Enums;

namespace ShoeStore.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, ErrorMessage = "Mật khẩu không được vượt quá 100 ký tự")]
        public string PasswordHash { get; set; }

        [NotMapped]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6-50 ký tự")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$", 
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ thường, 1 chữ hoa, 1 số và 1 ký tự đặc biệt")]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z\s\u0100-\u017F\u0180-\u024F\u1E00-\u1EFF]+$", ErrorMessage = "Họ tên chỉ được chứa chữ cái và khoảng trắng")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Số điện thoại chỉ được chứa số")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        public bool Status { get; set; } = true;

        [Display(Name = "Ngày đăng ký")]
        public DateTime RegisterDate { get; set; } = DateTime.Now;

        [Display(Name = "Lần đăng nhập cuối")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool EmailConfirmed { get; set; } = false;
        
        public string? EmailConfirmationToken { get; set; }
        
        public DateTime? EmailConfirmationTokenExpiry { get; set; }

        [Required]
        [Display(Name = "Vai trò")]
        public int RoleID { get; set; }  // Phân quyền người dùng (Admin, User)

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }  // Quan hệ với bảng Role để phân quyền

        public int? MemberRankId { get; set; }  // Hạng thành viên (Bronze, Silver, Gold)
        
        [ForeignKey("MemberRankId")]
        public virtual MemberRank MemberRank { get; set; }  // Quan hệ với bảng MemberRank để xác định hạng thành viên

        public decimal TotalSpent { get; set; } = 0;  // Tổng tiền đã chi tiêu, dùng để tính hạng thành viên

        public virtual ICollection<Order> Orders { get; set; }
    }
}
