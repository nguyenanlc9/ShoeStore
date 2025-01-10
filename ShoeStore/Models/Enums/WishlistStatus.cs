using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.Enums
{
    public enum WishlistStatus
    {
        [Display(Name = "Đang bán")]
        InStock = 1,

        [Display(Name = "Hết hàng")]
        OutOfStock = 2,

        [Display(Name = "Ngừng kinh doanh")]
        Discontinued = 3
    }
}