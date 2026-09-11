using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodOrderitem
{
    public string OrderItemId { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string MenuId { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string? SelectedOptions { get; set; }
    public decimal? ExtraOptionPrice { get; set; }

    public virtual FoodMenu Menu { get; set; } = null!;
    public virtual FoodOrder Order { get; set; } = null!;
}