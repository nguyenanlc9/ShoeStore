using ShoeStore.Models;
using ShoeStore.Models.Enums;

public class CheckoutViewModel
{
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public int ProvinceCode { get; set; }
    public int DistrictCode { get; set; }
    public int WardCode { get; set; }
    public string AddressDetail { get; set; }
    public string Notes { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    // Các property cho tính toán giá
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CouponDiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal FinalTotal { get; set; }
    public int? MemberRankDiscountPercent { get; set; }
    public string MemberRankName { get; set; }
    
    // Property cho địa chỉ đã format theo GHN
    public string FormattedAddress { get; set; }

    // Property cho danh sách sản phẩm trong giỏ hàng
    public List<CartItem> CartItems { get; set; }

    // Property cho coupon đã áp dụng
    public Coupon AppliedCoupon { get; set; }

    public decimal ShipFeeGHN { get; set; }
} 