using System.Text.Json.Serialization;

namespace ShoeStore.Models.APIAddress
{
    public class District
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("wards")]
        public List<Ward> Wards { get; set; } = new List<Ward>();
    }
} 