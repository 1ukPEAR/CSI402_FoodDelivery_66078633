namespace WebApplication1.FoodViewModels
{
    public class MenuViewModel
    {
        public string MenuName { get; set; } = string.Empty;
        public decimal MenuPrice { get; set; }
        public ulong? MenuStatus { get; set; }
        public IFormFile? MenuImageFile { get; set; }
        public string OptionId { get; set; } = string.Empty;
        public List<string>? OptionName { get; set; }
        public List<decimal?>? ExtraPrice { get; set; }

        public string MenuTypeId { get; set; } = string.Empty;
        public string MenuTypeName { get; set; } = string.Empty;
    }
}