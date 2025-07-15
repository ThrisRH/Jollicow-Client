using Microsoft.AspNetCore.Mvc;
using Jollicow.Services;

namespace Jollicow.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly TokenService _tokenService;
        private readonly FirebaseService _firebaseService;

        public CartController(ILogger<CartController> logger, TokenService tokenService)
        {
            _logger = logger;
            _tokenService = tokenService;
        }

        [HttpGet("/cart/cartdetail")]
        public IActionResult CartDetail(string acsc)
        {
            // Lấy thông tin từ mã hóa
            var decrypted = _tokenService.TryDecrypt(acsc);
            if (decrypted == null)
            {
                ViewBag.Error = "Link không hợp lệ hoặc đã hết hạn.";
                return View("Error");
            }

            var (id_table, restaurant_id) = decrypted.Value;

            if (string.IsNullOrEmpty(id_table) || string.IsNullOrEmpty(restaurant_id))
            {
                _logger.LogWarning("Missing parameters: id_table={IdTable}, restaurant_id={IdRestaurant}", id_table, restaurant_id);
                return BadRequest("Thiếu thông tin.");
            }

            ViewData["IdTable"] = id_table;
            ViewData["RestaurantId"] = restaurant_id;
            return View();
        }
    }
}