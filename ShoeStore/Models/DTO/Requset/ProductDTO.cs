using System.ComponentModel.DataAnnotations;

namespace ShoeStore.Models.DTO.Requset
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }

        public int BrandId { get; set; }

        public string? Name { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Giá không thể là số âm.")]
        public decimal Price { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Giá giảm không thể là số âm.")]
        public decimal DiscountPrice { get; set; }
        public string Description { get; set; }
        public IFormFile? ImagePath { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không thể là số âm.")]
        public int StockQuantity { get; set; }


    }
}
