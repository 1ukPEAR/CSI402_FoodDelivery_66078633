namespace WebApplication1.FoodViewModels
{
    public class ReviewViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public byte Score { get; set; }
        public string? CommentReview { get; set; }
    }
}