using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Username không được để trống")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [NotMapped]
        public string? NewPassword { get; set; }

        [Display(Name = "Họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string Phone { get; set; }

        public bool Status { get; set; } = true;

        [Display(Name = "Ngày đăng ký")]
        public DateTime RegisterDate { get; set; } = DateTime.Now;

        [Display(Name = "Lần đăng nhập cuối")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Vai trò")]
        public int RoleID { get; set; }

        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }

        public int? MemberRankId { get; set; }
        
        [ForeignKey("MemberRankId")]
        public virtual MemberRank MemberRank { get; set; }

        public decimal TotalSpent { get; set; }  // Tổng số tiền đã chi tiêu
    }
}
