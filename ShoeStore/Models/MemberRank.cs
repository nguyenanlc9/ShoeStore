using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using ShoeStore.Models;

public class MemberRank
{
    [Key]
    public int RankId { get; set; }

    [Required]
    [StringLength(50)]
    public string RankName { get; set; }  // Bronze, Silver, Gold, Platinum...

    [Required]
    public decimal MinimumSpent { get; set; }  // Số tiền tối thiểu để đạt hạng

    [Required]
    [Range(0, 100)]
    public int DiscountPercent { get; set; }  // Phần trăm giảm giá

    public string? Description { get; set; }

    public string? BadgeImage { get; set; }  // Icon/hình ảnh cho rank

    public virtual ICollection<User> Users { get; set; }
} 