using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodPromotion
{
    public string PromotionId { get; set; } = null!;
    public string? PromotionName { get; set; }
    public string? DescriptionPro { get; set; }
    public sbyte? DiscountPercent { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int? MaxUsePerUser { get; set; }
    public int? MaxUsePerOrder { get; set; }
    public bool IsMilestonePromotion { get; set; }
    public int? RequiredOrderCount { get; set; }

    public virtual ICollection<FoodOrder> FoodOrders { get; set; } = new List<FoodOrder>();
    public virtual ICollection<FoodMenu> Menus { get; set; } = new List<FoodMenu>();
    public virtual ICollection<FoodPromotionMenu> FoodPromotionMenus { get; set; } = new List<FoodPromotionMenu>();
    public virtual ICollection<FoodPromotionUsage> FoodPromotionUsages { get; set; } = new List<FoodPromotionUsage>();
}