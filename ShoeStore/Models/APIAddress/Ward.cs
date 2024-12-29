using System.Text.Json.Serialization;

namespace ShoeStore.Models.APIAddress
{
    public class Ward
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
} 