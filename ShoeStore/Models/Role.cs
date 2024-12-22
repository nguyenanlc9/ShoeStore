using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        [Display(Name = "Role ID")]
        public int Role_Id { get; set; }
        [Display(Name = "Name")]
        public string Role_Name { get; set; }
    }
}
