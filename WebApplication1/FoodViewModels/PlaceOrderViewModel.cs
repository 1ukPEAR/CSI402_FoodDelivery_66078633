namespace WebApplication1.FoodViewModels
{
    public class PlaceOrderViewModel
    {
        public string? SelectedAddressId { get; set; }
        public bool UseNewAddress { get; set; }
        public string? NewReceiverName { get; set; }
        public string? NewReceiverPhone { get; set; }
        public string? NewAddressDetail { get; set; }
        public string? NewSubDistrict { get; set; }
        public string? NewDistrict { get; set; }
        public string? NewProvince { get; set; }
        public bool SaveNewAddressToBook { get; set; } = true;
    }
}