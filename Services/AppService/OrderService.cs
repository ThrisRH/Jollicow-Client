using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jollicow.Models;
using Newtonsoft.Json;

// Lớp request
public class OrderRequest
{
    [JsonProperty("id_table")]
    public string id_table { get; set; } = String.Empty;
    [JsonProperty("id_restaurant")]
    public string id_restaurant { get; set; } = String.Empty;
    [JsonProperty("payment")]
    public string pyamentMethod { get; set; } = String.Empty;
}
public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ToppingService> _logger;

    public OrderService(HttpClient httpClient, ILogger<ToppingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task CreateOrder(string id_table, string id_restaurant)
    {
        var request = new OrderRequest
        {
            id_table = id_table,
            id_restaurant = id_restaurant,
            pyamentMethod = "Thanh toán trực tiếp"
        };

        var url = $"https://jollicowfe-production.up.railway.app/api/admin/carts/createALL";
        var response = await _httpClient.PostAsJsonAsync(url, request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy topping cho bàn {0} - nhà hàng {1}", request.id_table, request.id_restaurant);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Tạo đơn hàng thất bại: {response.StatusCode}");
        }

        // Nếu bạn vẫn muốn log dữ liệu trả về (nếu có):
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Phản hồi từ server: {0}", content);
    }
}