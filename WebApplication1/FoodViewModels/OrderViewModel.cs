namespace WebApplication1.FoodViewModels
{
    public class OrderViewModel
    {
        public string OrderId { get; set; }= string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string StatusOrder { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public TimeOnly OrderTime { get; set; }
        public string OrderList { get; set; } = string.Empty;
        public double TotalPrice { get; set; }
        public string PromotionId { get; set; } = string.Empty;
        public double DiscountAmount { get; set; }
        public double FinalPrice { get; set; }
        public bool IsTakeAway { get; set; }
    }
}