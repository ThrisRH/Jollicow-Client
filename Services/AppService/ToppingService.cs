using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jollicow.Models;
using Newtonsoft.Json;

// Lớp request
public class DishRequest
{
    [JsonProperty("id_dishes")]
    public string id_dishes { get; set; } = String.Empty;
}
public class ToppingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ToppingService> _logger;

    public ToppingService(HttpClient httpClient, ILogger<ToppingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ToppingModels>> GetToppings(string id_dishes)
    {
        var request = new DishRequest
        {
            id_dishes = id_dishes
        };

        var url = $"https://jollicowfe-production.up.railway.app/api/admin/toppings/filter";
        var response = await _httpClient.PostAsJsonAsync(url, request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy topping cho món {0}", request.id_dishes);
            return new List<ToppingModels>();
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Lấy topping thất bại: {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<List<ToppingModels>>(content);

        // Tự động set mustChoice = true cho các topping về kích thước/kích cỡ
        if (data != null)
        {
            foreach (var topping in data)
            {
                var nameLower = topping.name_details.ToLower();

                // Set mustChoice = true cho các topping về kích thước/kích cỡ
                // Override bất kể API trả về mustChoose = true hay false
                if (nameLower.Contains("kích thước") ||
                    nameLower.Contains("kích cỡ") ||
                    nameLower.Contains("size") ||
                    nameLower.Contains("độ lớn") ||
                    nameLower.Contains("dụng cụ") ||
                    nameLower.Contains("utensil"))
                {
                    topping.mustChoice = true;
                }
            }
        }

        return data ?? new List<ToppingModels>();
    }
}