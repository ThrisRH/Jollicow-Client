using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jollicow.Models;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Lớp request
public class OrderRequest
{
    [JsonProperty("id_table")]
    public string id_table { get; set; } = String.Empty;
    [JsonProperty("id_restaurant")]
    public string id_restaurant { get; set; } = String.Empty;
    [JsonProperty("payment")]
    public string pyamentMethod { get; set; } = String.Empty;
    [JsonProperty("id_promotion")]
    public string? id_promotion { get; set; }
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

    public async Task<String?> CreateOrder(string id_table, string id_restaurant, string? voucherId = null, string? paymentMethod = null)
    {
        _logger.LogInformation($"Voucher ID: {voucherId ?? "null"}");
        var request = new OrderRequest
        {
            id_table = id_table,
            id_restaurant = id_restaurant,
            pyamentMethod = paymentMethod == "cash" ? "Tiền mặt" : "Thanh toán trực tiếp",
            id_promotion = voucherId
        };

        var url = $"https://jollicowfe-production.up.railway.app/api/admin/carts/createALL";
        var response = await _httpClient.PostAsJsonAsync(url, request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy topping cho bàn {0} - nhà hàng {1}", request.id_table, request.id_restaurant);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Tạo đơn hàng thất bại: {response.StatusCode}");
        }

        // Nếu bạn vẫn muốn log dữ liệu trả về (nếu có):
        var contentString = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Phản hồi từ server: {0}", contentString);

        // Giả sử response trả về dạng JSON: { "id_order": "123abc", ... }
        var contentObj = JsonConvert.DeserializeObject<JObject>(contentString);
        string? orderId = contentObj?["id_order"]?.ToString();

        if (paymentMethod == "vnpay" && orderId != null)
        {
            _logger.LogInformation($"Chuẩn bị redirect tới VNPay với mã đơn: {orderId}");

            var payment_url = $"https://jollicowbe-admin.up.railway.app/api/admin/pay/create_payment_url";
            var url_response = await _httpClient.PostAsJsonAsync(payment_url, new
            {
                id_order = orderId,
            });

            if (!url_response.IsSuccessStatusCode)
            {
                _logger.LogError($"Lỗi khi tạo URL thanh toán VNPay: {url_response.StatusCode}");
                throw new Exception($"Lỗi khi tạo URL thanh toán VNPay: {url_response.StatusCode}");
            }

            var json = await url_response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var paymentUrl = jsonDoc.RootElement.GetProperty("paymentUrl").GetString();

            if (string.IsNullOrEmpty(paymentUrl))
            {
                _logger.LogError("Không nhận được URL thanh toán từ VNPay");
                throw new Exception("Không nhận được URL thanh toán từ VNPay");
            }

            return paymentUrl;
        }
        return null;
    }
}