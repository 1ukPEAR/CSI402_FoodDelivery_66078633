using WebApplication1.Models.db;

namespace WebApplication1.FoodViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new();
        public List<FoodAddress> Addresses { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
    }
}