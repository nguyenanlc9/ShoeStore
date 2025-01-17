using Newtonsoft.Json;

namespace ShoeStore.Models.GHN
{
    public class GHN_OrderItem
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("code")]
        public required string Code { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }
    }
} 