using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Jollicow.Services;
using Jollicow.Models;

namespace Jollicow.Controllers
{
    public class AuthModels
    {
        public bool exists { get; set; }
    }

    public class MenuController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MenuController> _logger;
        private readonly IMemoryCache _cache;
        private const int CACHE_DURATION_MINUTES = 30;

        // Serivce
        private readonly TokenService _tokenService;
        private readonly ToppingService _toppingService;

        public MenuController(
            IHttpClientFactory httpClientFactory,
            ILogger<MenuController> logger,
            TokenService tokenService,
            IMemoryCache cache,
            ToppingService toppingService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _cache = cache;
            _tokenService = tokenService;
            _toppingService = toppingService;
        }

        // Đường dẫn tạo mã hóa
        [HttpGet("/menu/generate")]
        public IActionResult GenerateToken(string id_table, string restaurant_id)
        {
            if (string.IsNullOrEmpty(id_table) || string.IsNullOrEmpty(restaurant_id))
            {
                return BadRequest("Thiếu thông tin.");
            }

            var token = _tokenService.GenerateToken(id_table, restaurant_id);
            var url = Url.Action("Menu", new { acsc = token }); // Access code
            return Redirect(url!);
        }

        [HttpGet("/menu")]
        public async Task<IActionResult> Menu(string acsc, string id_category)
        {
            var decrypted = _tokenService.TryDecrypt(acsc);
            if (decrypted == null)
            {
                ViewBag.Error = "Link không hợp lệ hoặc đã hết hạn.";
                return View("Error");
            }

            // Lấy thông tin từ mã hóa

            var (id_table, restaurant_id) = decrypted.Value;

            // Kiểm tra session
            var sessionKey = $"auth_{id_table}_{restaurant_id}";

            if (HttpContext.Session.GetString(sessionKey) == "true")
            {
                _logger.LogInformation("Session exists, load menu");
                return await RenderMenu(id_table, restaurant_id, id_category);
            }
            _logger.LogInformation("Session not exists, this is a first time! Check first!");

            // Gọi API
            var client = _httpClientFactory.CreateClient();
            var postData = new
            {
                id_table = id_table,
                restaurant_id = restaurant_id
            };

            var response = await client.PostAsJsonAsync("https://jollicowfe-production.up.railway.app/api/admin/tables/checkauth", postData);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthModels>();
                if (result?.exists == true)
                {
                    HttpContext.Session.SetString("id_table", id_table);
                    HttpContext.Session.SetString("restaurant_id", restaurant_id);
                    HttpContext.Session.SetString(sessionKey, "true");
                    _logger.LogInformation("✅ Session đã gán: {SessionKey}", sessionKey);
                    _logger.LogInformation("Session Keys: " + string.Join(", ", HttpContext.Session.Keys));

                    return await RenderMenu(id_table, restaurant_id, id_category);
                }
            }
            ViewBag.Error = "Không thể xác thực bàn/người dùng.";
            return View("Error");
        }

        private async Task<IActionResult> RenderMenu(string id_table, string restaurant_id, string id_category)
        {
            var firebaseService = new FirebaseService();

            // Make cache key restaurant-specific
            var categoriesCacheKey = $"categories_{restaurant_id}";

            _logger.LogInformation("Loading categories for restaurant: {RestaurantId} with cache key: {CacheKey}", restaurant_id, categoriesCacheKey);

            var allCategories = await _cache.GetOrCreateAsync(categoriesCacheKey, async entry =>
             {
                 entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES);
                 _logger.LogInformation("Cache miss for restaurant {RestaurantId}, fetching from Firebase", restaurant_id);
                 return await firebaseService.GetCategoriesByRestaurantIdAsync(restaurant_id);
             });

            _logger.LogInformation("Loaded {CategoryCount} categories for restaurant {RestaurantId}: {@Categories}",
                allCategories.Count, restaurant_id, allCategories.Select(c => new { c.id_category, c.name }));

            if (string.IsNullOrEmpty(id_category) && allCategories.Any())
            {
                id_category = allCategories.First().id_category;
                _logger.LogInformation("No category selected, defaulting to first category: {CategoryId}", id_category);
            }

            var dishes = await firebaseService.GetAllDishesAsync();
            var categories = await firebaseService.GetAllCategoriesAsync();

            _logger.LogInformation("Dishes: {@Dishes}, restaurant_id: {@RestaurantId}", dishes, restaurant_id);
            dishes = dishes.Where(d => d.id_category == id_category && d.restaurant_id == restaurant_id).ToList();

            _logger.LogInformation("Filtered dishes for category {CategoryId} and restaurant {RestaurantId}: {DishCount} dishes",
                id_category, restaurant_id, dishes.Count);

            var menuViewModel = new MenuViewModel
            {
                Dishes = dishes,
                Categories = allCategories,
                SelectedCategoryID = id_category,
                RestaurantId = restaurant_id,
                IdTable = id_table,
            };

            ViewData["IdTable"] = id_table ?? "";
            ViewData["RestaurantId"] = restaurant_id ?? "";

            return View("Menu", menuViewModel);
        }

        [HttpGet("/menu/dish")]
        public async Task<IActionResult> DishDetail(string acsc, string id_dishes)
        {
            // Lấy thông tin từ mã hóa
            var decrypted = _tokenService.TryDecrypt(acsc);
            if (decrypted == null)
            {
                ViewBag.Error = "Link không hợp lệ hoặc đã hết hạn.";
                return View("Error");
            }

            var (id_table, restaurant_id) = decrypted.Value;


            // Xử lý đường dẫn
            if (string.IsNullOrEmpty(id_table) || string.IsNullOrEmpty(restaurant_id) || string.IsNullOrEmpty(id_dishes))
            {
                _logger.LogWarning("Missing parameters: id_table={IdTable}, restaurant_id={IdRestaurant}, id_dishes={IdDishes}", id_table, restaurant_id, id_dishes);
                return BadRequest("Thiếu thông tin.");
            }

            var firebaseService = new FirebaseService();
            var dish = await firebaseService.GetDishByIdAsync(id_dishes);

            _logger.LogInformation("Dish found: {@Dish}", dish);

            if (dish == null)
            {
                return NotFound();
            }

            // Lấy topping
            var toppings = await _toppingService.GetToppings(id_dishes);
            _logger.LogInformation("Toppings: {@Toppings}", toppings.Select(t => new { t.id_topping, t.name_details, t.options }));

            ViewData["IdTable"] = id_table ?? "";
            ViewData["RestaurantId"] = restaurant_id ?? "";
            ViewData["Toppings"] = toppings ?? new List<ToppingModels>();

            return View(dish);
        }

        [HttpGet("/menu/getdishesbycategory")]
        public async Task<IActionResult> GetDishesByCategory(string id_category, string restaurant_id)
        {
            if (string.IsNullOrEmpty(id_category) || string.IsNullOrEmpty(restaurant_id))
            {
                return BadRequest("Thiếu thông tin.");
            }

            var firebaseService = new FirebaseService();
            var dishes = await firebaseService.GetAllDishesAsync();
            var filtered = dishes.Where(d => d.id_category == id_category && d.restaurant_id == restaurant_id).ToList();

            return PartialView("_DishListPartial", filtered);
        }
    }
}
