using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.FoodViewModels
{
    public class CartItemViewModel
    {
        public string MenuId { get; set; } = string.Empty;
        public string MenuName { get; set; } = string.Empty;
        public string? MenuImage { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public List<string> SelectedOptionIds { get; set; } = new();
        public List<string> SelectedOptionNames { get; set; } = new();
        public List<string> SelectedOptionDisplayNames { get; set; } = new();
        public decimal ExtraPriceTotal { get; set; }
        public string? PromotionId { get; set; }
        public string? PromotionName { get; set; }
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal OriginalFinalPrice => Price + ExtraPriceTotal;
        public decimal DiscountAmount
        {
            get
            {
                if (!HasPromotion) return 0;

                var type = (DiscountType ?? "").Trim().ToUpper();

                if (type == "AMOUNT")
                {
                    return DiscountValue > OriginalFinalPrice ? OriginalFinalPrice : DiscountValue;
                }
                var percent = DiscountPercent > 0 ? DiscountPercent : DiscountValue;
                var discount = OriginalFinalPrice * percent / 100m;

                return discount > OriginalFinalPrice ? OriginalFinalPrice : discount;
            }
        }
        public decimal FinalPrice
        {
            get
            {
                var result = OriginalFinalPrice - DiscountAmount;
                return result < 0 ? 0 : result;
            }
        }
        public decimal OriginalTotalPrice => OriginalFinalPrice * Quantity;
        public decimal TotalPrice => FinalPrice * Quantity;
        public decimal TotalDiscountAmount => DiscountAmount * Quantity;
        public bool HasPromotion => !string.IsNullOrWhiteSpace(PromotionId);
        public string OptionSummary =>
            SelectedOptionNames != null && SelectedOptionNames.Any()
                ? string.Join(", ", SelectedOptionNames)
                : "-";
        public string OptionDisplaySummary =>
            SelectedOptionDisplayNames != null && SelectedOptionDisplayNames.Any()
                ? string.Join(", ", SelectedOptionDisplayNames)
                : "-";
        public string OptionKey =>
            SelectedOptionIds != null && SelectedOptionIds.Any()
                ? string.Join("|", SelectedOptionIds.OrderBy(x => x))
                : "NO_OPTION";
    }
}