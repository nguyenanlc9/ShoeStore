using ShoeStore.Models;

namespace ShoeStore.Models.ViewModels
{
    public class ProductViewModel
    {
        public Product Product { get; set; }
        public List<decimal> AvailableSizes { get; set; }
    }
} 