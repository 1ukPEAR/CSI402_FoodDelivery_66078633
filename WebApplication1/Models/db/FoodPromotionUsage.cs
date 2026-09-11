using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodPromotionUsage
{
    public string UsageId { get; set; } = null!;
    public string PromotionId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? OrderId { get; set; }
    public DateTime UsedAt { get; set; }
    public int UsedQty { get; set; }

    public virtual FoodPromotion Promotion { get; set; } = null!;
    public virtual FoodUser User { get; set; } = null!;
    public virtual FoodOrder? Order { get; set; }
}