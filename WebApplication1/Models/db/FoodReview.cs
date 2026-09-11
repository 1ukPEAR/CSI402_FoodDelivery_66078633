namespace WebApplication1.Models.db
{
    public partial class FoodReview
    {
        public string ReviewId { get; set; } = null!;
        public string? OrderId { get; set; }
        public string UserId { get; set; } = null!;
        public byte Score { get; set; }
        public string? CommentReview { get; set; }
        public DateTime? ReviewDate { get; set; }
        
        public virtual FoodUser UserReview { get; set; } = null!;
    }
}