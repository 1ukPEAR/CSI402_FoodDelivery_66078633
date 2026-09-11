using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodPromotionMenu
{
    public string PromotionId { get; set; } = null!;
    public string MenuId { get; set; } = null!;

    public virtual FoodPromotion Promotion { get; set; } = null!;
    public virtual FoodMenu Menu { get; set; } = null!;
}