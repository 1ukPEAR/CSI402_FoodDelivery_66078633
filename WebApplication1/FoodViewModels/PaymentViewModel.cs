using Microsoft.AspNetCore.Http;

namespace WebApplication1.FoodViewModels
{
    public class PaymentViewModel
    {
        public string? PaymentMethod { get; set; }
        public IFormFile? SlipFile { get; set; }
        public string? OrderNote { get; set; }
        public string? PromotionId { get; set; }
        public decimal? DiscountAmount { get; set; }
    }
}