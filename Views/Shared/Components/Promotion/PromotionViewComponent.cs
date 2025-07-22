using Microsoft.AspNetCore.Mvc;
using Jollicow.Models;

namespace Jollicow.Views.Shared.Components.Promotion
{
    public class PromotionViewComponent : ViewComponent
    {
        private readonly PromotionService _promotionService;
        private readonly ILogger<PromotionViewComponent> _logger;

        public PromotionViewComponent(PromotionService promotionService, ILogger<PromotionViewComponent> logger)
        {
            _promotionService = promotionService;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync(string restaurantId)
        {
            var restaurantIdTrim = restaurantId.Trim();
            var promotions = await _promotionService.GetPromotions(restaurantIdTrim);
            return View(promotions);
        }
    }
}

