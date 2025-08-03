using Microsoft.AspNetCore.Mvc;
using Jollicow.Services;
using Jollicow.Models;

namespace Jollicow.Controllers
{
    public class PaymentController : Controller
    {
        private readonly TokenService _tokenService;
        private readonly OrderService _orderService;
        private readonly ILogger<PaymentController> _logger;



        public PaymentController(TokenService tokenService, OrderService orderService, ILogger<PaymentController> logger)
        {
            _tokenService = tokenService;
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> VietQR(string acsc)
        {
            // Viết hàm get payment info từ db
            // Data tạm
            var paymentInfo = new PaymentInfo
            {
                AccountName = "Trần Hữu Minh Trí",
                AccountNumber = "0389105492",
                Bank = "MB",
            };
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

            // Lấy tổng tiền từ giỏ hàng
            var cartTotal = await CartAPIHelper.GetCartTotalAsync(idTable, restaurantId);

            ViewData["IdTable"] = idTable;
            ViewData["RestaurantId"] = restaurantId;
            ViewData["Amount"] = cartTotal;
            ViewData["PaymentInfo"] = paymentInfo;
            ViewData["Description"] = $"Table {idTable} - Thanh toán hóa đơn";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Cash(string acsc)
        {
            // Lấy thông tin từ mã hóa
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

            // Lấy tổng tiền từ giỏ hàng
            var cartTotal = await CartAPIHelper.GetCartTotalAsync(idTable, restaurantId);

            ViewData["IdTable"] = idTable;
            ViewData["RestaurantId"] = restaurantId;
            ViewData["Amount"] = cartTotal;
            return View();
        }

        //  Tạo order áp dụng cho VietQR
        [HttpPost]
        public async Task<IActionResult> Create(string acsc, string? voucherId = null, string? paymentMethod = null)
        {
            try
            {
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

                _logger.LogInformation($"Voucher ID: {voucherId ?? "null"}");

                if (paymentMethod == "vnpay")
                {
                    try
                    {
                        var vnpay_url = await _orderService.CreateOrder(idTable, restaurantId, voucherId, paymentMethod);
                        if (!string.IsNullOrEmpty(vnpay_url))
                        {
                            _logger.LogInformation($"VNPay URL: {vnpay_url}");

                            // Trả về JSON response thay vì redirect trực tiếp
                            return Json(new { success = true, paymentUrl = vnpay_url });
                        }
                        else
                        {
                            _logger.LogError("Không nhận được URL thanh toán VNPay");
                            return Json(new { success = false, error = "Lỗi khi tạo đơn hàng." });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi tạo đơn hàng VNPay: {ex.Message}");
                        return Json(new { success = false, error = $"Lỗi khi tạo đơn hàng: {ex.Message}" });
                    }
                }
                else if (paymentMethod == "cash")
                {
                    await _orderService.CreateOrder(idTable, restaurantId, voucherId, "cash");
                }
                else
                {
                    await _orderService.CreateOrder(idTable, restaurantId, voucherId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi trong PaymentController.Create: {ex.Message}");
                TempData["Error"] = $"Lỗi khi đặt đơn: {ex.Message}";
            }

            return RedirectToAction("Confirmation", "Payment", new { acsc = acsc });
        }


        [HttpGet]
        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
