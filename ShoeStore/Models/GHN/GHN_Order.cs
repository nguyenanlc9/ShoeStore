using Newtonsoft.Json;

namespace ShoeStore.Models.GHN
{
    public class GHN_Order
    {
        [JsonProperty("to_name")]
        public required string ToName { get; set; }

        [JsonProperty("from_name")]
        public required string FromName { get; set; }

        [JsonProperty("from_phone")]
        public required string FromPhone { get; set; }

        [JsonProperty("from_address")]
        public required string FromAddress { get; set; }

        [JsonProperty("from_ward_name")]
        public required string FromWardName { get; set; }

        [JsonProperty("from_district_name")]
        public required string FromDistrictName { get; set; }

        [JsonProperty("from_provice_name")]
        public required string FromProviceName { get; set; }

        [JsonProperty("to_phone")]
        public required string ToPhone { get; set; }

        [JsonProperty("to_address")]
        public required string ToAddress { get; set; }

        [JsonProperty("to_ward_code")]
        public required string ToWardCode { get; set; }

        [JsonProperty("to_district_id")]
        public required int ToDistrictId { get; set; }

        [JsonProperty("client_order_code")]
        public required string ClientOrderCode { get; set; }

        [JsonProperty("payment_type_id")]
        public int PaymentTypeId { get; set; } = 2; // 2 for Buyer pays

        [JsonProperty("required_note")]
        public string RequiredNote { get; set; } = "KHONGCHOXEMHANG";

        [JsonProperty("cod_amount")]
        public int CodAmount { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("service_type_id")]
        public int ServiceTypeId { get; set; } = 2; // 2 for Standard delivery

        [JsonProperty("pick_shift")]
        public int[] PickShift { get; set; } = [2];

        [JsonProperty("Items")]
        public required List<GHN_OrderItem> Items { get; set; }
    }
} 