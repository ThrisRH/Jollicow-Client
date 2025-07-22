public class CartService : ICartService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ToppingService> _logger;

    public CartService(HttpClient httpClient, ILogger<ToppingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task DeleteCart(String cartID)
    {
        using (_httpClient)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"https://jollicowfe-production.up.railway.app/api/carts/{cartID}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Xóa giỏ hàng thành công");
            }
            else
            {
                _logger.LogError("Xóa giỏ hàng thất bại");
            }
        }
    }
}