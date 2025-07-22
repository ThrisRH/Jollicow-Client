using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// Model chính của promotion
public class PromotionModel
{
    [JsonProperty("id_promotion")]
    public string IdPromotion { get; set; }

    [JsonProperty("id_restaurant")]
    public string IdRestaurant { get; set; }

    [JsonProperty("date_end")]
    public DateTime DateEnd { get; set; }

    [JsonProperty("max_discount")]
    public int MaxDiscount { get; set; }

    [JsonProperty("min_order_value")]
    public int MinOrderValue { get; set; }

    [JsonProperty("percent")]
    public int Percent { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("title_sub")]
    public string TitleSub { get; set; }
}

// Request gửi lên server
public class PromotionRequest
{
    [JsonProperty("id_restaurant")]
    public string id_restaurant { get; set; } = string.Empty;
}

// Wrapper cho response JSON
public class PromotionResponse
{
    [JsonProperty("data")]
    public List<PromotionModel> Data { get; set; }
}

// Service gọi API
public class PromotionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(HttpClient httpClient, ILogger<PromotionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<PromotionModel>> GetPromotions(string id_restaurant)
    {
        _logger.LogInformation("Lấy promotion cho nhà hàng {0}", id_restaurant.Split(" "));

        var request = new PromotionRequest
        {
            id_restaurant = id_restaurant
        };

        var url = $"https://jollicowfe-production.up.railway.app/api/admin/promotions";

        var response = await _httpClient.PostAsJsonAsync(url, request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy promotion cho nhà hàng {0}", id_restaurant);
            return new List<PromotionModel>();
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Lấy promotion thất bại: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Dữ liệu JSON raw:\n{0}", content);


        var dict = JsonConvert.DeserializeObject<Dictionary<string, PromotionModel>>(content);
        var list = dict?.Values.ToList() ?? new List<PromotionModel>();

        _logger.LogInformation("Số lượng promotion lấy được: {0}", list.Count);
        return list;
    }
}
