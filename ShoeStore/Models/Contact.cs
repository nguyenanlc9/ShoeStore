using Microsoft.Extensions.FileProviders;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class Contact
    {
        [Key]
        public string? ContactName { get; set; }
        public string? ContactMap { get; set; }

        [Required(ErrorMessage = "Phone là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = " Phone phải là số.")]
        public string? ContactPhone { get; set; }
        
        public string? ContactEmail { get; set; }
        
        public string ContactDescription { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
