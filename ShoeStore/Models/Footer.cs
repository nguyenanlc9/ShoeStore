using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeStore.Models
{
    public class Footer
    {
        [Key]
        public int FooterId { get; set; }
        public string FooterAddress { get; set; }
        public string FooterPhone { get; set; }
        public string FooterEmail { get; set; }
    }
}
