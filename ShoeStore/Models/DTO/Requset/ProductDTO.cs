namespace ShoeStore.Models.DTO.Requset
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? Description { get; set; }
        public IFormFile? ImagePath { get; set; }
        public int StockQuantity { get; set; }
    }
}
