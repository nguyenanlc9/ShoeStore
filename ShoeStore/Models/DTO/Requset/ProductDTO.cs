namespace ShoeStore.Models.DTO.Requset
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int BrandId { get; set; }
        public string BrandName { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? Description { get; set; }
        public string ImagePath { get; set; }
        public int StockQuantity { get; set; }
    }
}
