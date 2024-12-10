using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoesStore.Models
{
    [Table("AdminUsers")]
    public class AdminUser
    {

        [Key]
        public int AdminID { get; set; }

        [Required(ErrorMessage = "Username không được để trống")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password không được để trống")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Họ và tên")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)]
        public string Email { get; set; }

        public bool Status { get; set; } = true;

        [Display(Name = "Lần đăng nhập cuối")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}