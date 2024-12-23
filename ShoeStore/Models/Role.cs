using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required(ErrorMessage = "Role Name không được để trống")]
        [StringLength(50)]
        public string RoleName { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
} 