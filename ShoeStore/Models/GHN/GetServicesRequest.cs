using System.Text.Json.Serialization;

namespace ShoeStore.Models.GHN
{
    public class GetServicesRequest
    {
        [JsonPropertyName("from_district")]
        public int from_district { get; set; }

        [JsonPropertyName("to_district")]
        public int to_district { get; set; }
    }
} 