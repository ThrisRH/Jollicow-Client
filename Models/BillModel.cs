using System.Text.Json.Serialization;

namespace Jollicow.Models
{
    public class BillResponseModel
    {
        // Dictionary với key là order ID và value là BillModel
        public Dictionary<string, BillModel> Orders { get; set; } = new Dictionary<string, BillModel>();
    }

    public class BillModel
    {
        [JsonPropertyName("status_order")]
        public string StatusOrder { get; set; } = "";

        [JsonPropertyName("id_table")]
        public string IdTable { get; set; } = "";

        [JsonPropertyName("date_create")]
        public DateTime DateCreate { get; set; }

        [JsonPropertyName("id_order")]
        public string IdOrder { get; set; } = "";

        [JsonPropertyName("default_price")]
        public decimal DefaultPrice { get; set; }

        [JsonPropertyName("total_price")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("items")]
        public List<BillItemModel> Items { get; set; } = new List<BillItemModel>();
    }

    public class BillItemModel
    {
        [JsonPropertyName("name_topping")]
        public List<string> NameTopping { get; set; } = new List<string>();

        [JsonPropertyName("note")]
        public string Note { get; set; } = "";

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("id_dishes")]
        public string IdDishes { get; set; } = "";
    }

    public class GetBillStatusRequest
    {
        [JsonPropertyName("idTable")]
        public string IdTable { get; set; } = "";

        [JsonPropertyName("restaurantId")]
        public string RestaurantId { get; set; } = "";
    }
}