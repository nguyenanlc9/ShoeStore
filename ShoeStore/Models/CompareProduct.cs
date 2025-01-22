using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models
{
    public class CompareProduct
    {
        [Key]
        public int CompareProductId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime AddedDate { get; set; }
    }
}