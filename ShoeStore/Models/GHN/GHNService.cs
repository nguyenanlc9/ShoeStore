using System.Text.Json.Serialization;

namespace ShoeStore.Models.GHN
{
    public class GHNServiceInfo
    {
        [JsonPropertyName("service_id")]
        public int ServiceId { get; set; }

        [JsonPropertyName("service_type_id")]
        public int ServiceTypeId { get; set; }

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }

        [JsonPropertyName("service_name")]
        public string ServiceName { get; set; }
    }
} 