using System.Text.Json.Serialization;

namespace ShoeStore.Models.Address
{
    public class Province
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("name_en")]
        public string NameEn { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("full_name_en")]
        public string FullNameEn { get; set; }

        [JsonPropertyName("code_name")]
        public string CodeName { get; set; }
    }

    public class District
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("name_en")]
        public string NameEn { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("full_name_en")]
        public string FullNameEn { get; set; }

        [JsonPropertyName("code_name")]
        public string CodeName { get; set; }

        [JsonPropertyName("province_code")]
        public int ProvinceCode { get; set; }
    }

    public class Ward
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("name_en")]
        public string NameEn { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("full_name_en")]
        public string FullNameEn { get; set; }

        [JsonPropertyName("code_name")]
        public string CodeName { get; set; }

        [JsonPropertyName("district_code")]
        public int DistrictCode { get; set; }
    }
} 