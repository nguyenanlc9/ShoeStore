using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    public class ContactUser
    {
        [Key]
        public int ContactUId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string? ContactUFirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? ContactULastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? ContactUEmail { get; set; }

        [MaxLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? ContactUAddress { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        [MaxLength(15, ErrorMessage = "Phone number cannot exceed 15 characters.")]
        public string? ContactUPhone { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters.")]
        public string? ContactUMessage { get; set; }
    }
}
