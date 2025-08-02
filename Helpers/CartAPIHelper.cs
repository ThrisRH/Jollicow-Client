using Jollicow.Models;
using System.Text.Json;

public class CartAPIHelper
{
    private static readonly HttpClient _httpClient = new HttpClient();

    // Call API to add to cart
    public static async Task<bool> AddCartAsync(CartModel cart)
    {
        var response = await _httpClient.PostAsJsonAsync("https://jollicowfe-production.up.railway.app/api/admin/carts/create", cart);
        Console.WriteLine(response.Content.ReadAsStringAsync());
        return response.IsSuccessStatusCode;
    }

    // Get cart total amount
    public static async Task<decimal> GetCartTotalAsync(string idTable, string restaurantId)
    {
        try
        {
            var payload = new
            {
                id_table = idTable,
                id_restaurant = restaurantId
            };

            var response = await _httpClient.PostAsJsonAsync("https://jollicowfe-production.up.railway.app/api/admin/carts/filter", payload);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CartFilterResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Carts != null)
                {
                    Console.WriteLine($"Cart items count: {result.Carts.Count}");
                    foreach (var item in result.Carts)
                    {
                        Console.WriteLine($"Item: Price={item.Price}, Quantity={item.Quantity}, Total={item.Price * item.Quantity}");
                    }
                    return result.Carts.Sum(item => (decimal)(item.Price * item.Quantity));
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting cart total: {ex.Message}");
            return 0;
        }
    }
}

// Response model for cart filter API
public class CartFilterResponse
{
    public List<CartItem> Carts { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}