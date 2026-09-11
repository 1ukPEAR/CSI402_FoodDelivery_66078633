using System.ComponentModel.DataAnnotations;

namespace WebApplication1.FoodViewModels
{
    public class PromotionViewModel
    {
        public string PromotionId { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกชื่อโปรโมชั่น")]
        public string PromotionName { get; set; } = string.Empty;

        public string? DescriptionPro { get; set; }
        public sbyte DiscountPercent { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกประเภทส่วนลด")]
        public string DiscountType { get; set; } = "PERCENT";

        [Range(typeof(decimal), "0.01", "999999", ErrorMessage = "ค่าส่วนลดต้องมากกว่า 0")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกวันเวลาเริ่มโปรโมชั่น")]
        public DateTime StartDatePromotion { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกวันเวลาสิ้นสุดโปรโมชั่น")]
        public DateTime EndDatePromotion { get; set; }
      
        public int? MaxUsePerUser { get; set; }
        public int? MaxUsePerOrder { get; set; }
        public bool IsMilestonePromotion { get; set; } = false;
        public int? RequiredOrderCount { get; set; }
        public List<string> SelectedMenuIds { get; set; } = new List<string>();
    }
}