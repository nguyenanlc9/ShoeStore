using Newtonsoft.Json;

namespace ShoeStore.Models.GHN
{
    public class GHN_Province
    {
        [JsonProperty("ProvinceID")]
        public int ProvinceId { get; set; }

        [JsonProperty("ProvinceName")]
        public string ProvinceName { get; set; }

        [JsonProperty("CountryID")]
        public int CountryId { get; set; }
    }

    public class GHN_District
    {
        [JsonProperty("DistrictID")]
        public int DistrictId { get; set; }

        [JsonProperty("DistrictName")]
        public string DistrictName { get; set; }

        [JsonProperty("ProvinceID")]
        public int ProvinceId { get; set; }
    }

    public class GHN_Ward
    {
        [JsonProperty("WardCode")]
        public string WardCode { get; set; }

        [JsonProperty("WardName")]
        public string WardName { get; set; }

        [JsonProperty("DistrictID")]
        public int DistrictId { get; set; }
    }
} 