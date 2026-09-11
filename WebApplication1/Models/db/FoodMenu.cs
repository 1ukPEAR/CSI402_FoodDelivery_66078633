using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodMenu
{
    public string MenuId { get; set; } = null!;
    public string MenuTypeId { get; set; } = null!;
    public string? MenuName { get; set; }
    public decimal? MenuPrice { get; set; }
    public string? MenuImage { get; set; }
    public ulong? MenuStatus { get; set; }
    public virtual ICollection<FoodMenuoption> FoodMenuoptions { get; set; } = new List<FoodMenuoption>();
    public virtual ICollection<FoodOrderitem> FoodOrderitems { get; set; } = new List<FoodOrderitem>();
    public virtual ICollection<FoodPromotionMenu> FoodPromotionMenus { get; set; } = new List<FoodPromotionMenu>();
    public virtual ICollection<FoodPromotion> Promotions { get; set; } = new List<FoodPromotion>();
    public virtual FoodMenutype MenuType { get; set; } = null!;
}
