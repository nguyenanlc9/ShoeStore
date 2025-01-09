using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ShoeStore.Models
{
    public class Blog
    {
        [Key]
        public int Blog_ID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
        public string Title { get; set; }

        [Display(Name = "Image")]
        public string? Img { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Display(Name = "Status")]
        public int Status { get; set; }

        [Display(Name = "Sort Order")]
        public int Sort { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Updated By")]
        public string? UpdatedBy { get; set; }

        // Navigation property for blog images
        public virtual ICollection<BlogImage>? BlogImages { get; set; }

        public Blog()
        {
            BlogImages = new HashSet<BlogImage>();
        }
    }
}