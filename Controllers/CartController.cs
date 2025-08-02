using Microsoft.AspNetCore.Mvc;
using Jollicow.Services;

namespace Jollicow.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly TokenService _tokenService;
        private readonly ICartService _cartService;
        private readonly OrderService _orderService;

        public CartController(ILogger<CartController> logger, TokenService tokenService, ICartService cartService, OrderService orderService)
        {
            _logger = logger;
            _tokenService = tokenService;
            _cartService = cartService;
            _orderService = orderService;
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

        [HttpDelete("/cart/deleteitem")]
        public async Task<IActionResult> DeleteItem(string id, string acsc)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Thiếu ID cần xoá.");
            }

            await _cartService.DeleteCart(id);

            return RedirectToAction("CartDetail", new { acsc = acsc });
        }


        // Tạo dành cho VN Pay
        [HttpPost]
        public async Task<IActionResult> Create(string acsc)
        {

            try
            {
                // Lấy thông tin từ mã hóa (giống như CartController và MenuController)
                var decrypted = _tokenService.TryDecrypt(acsc);
                if (decrypted == null)
                {
                    ViewBag.Error = "Link không hợp lệ hoặc đã hết hạn.";
                    return View("Error");
                }

                var (idTable, restaurantId) = decrypted.Value;

                if (string.IsNullOrEmpty(idTable) || string.IsNullOrEmpty(restaurantId))
                {
                    return BadRequest("Thiếu thông tin.");
                }
                await _orderService.CreateOrder(idTable, restaurantId);
                _logger.LogInformation($"Đơn hàng đã được tạo thành công cho Bàn {idTable} tại Nhà hàng {restaurantId}.");

            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi đặt đơn: {ex.Message}";
            }

            return RedirectToAction("Confirmation", "Payment", new { acsc = acsc });
        }
    }
}