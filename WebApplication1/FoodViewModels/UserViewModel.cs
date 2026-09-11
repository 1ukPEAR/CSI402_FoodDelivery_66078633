namespace WebApplication1.FoodViewModels
{
    public class UserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
        public ulong? UserStatus { get; set; }
        public DateTime? RegisterDate { get; set; }
        public string? RoleId { get; set; }
    }
}