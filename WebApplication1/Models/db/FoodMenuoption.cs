using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodMenuoption
{
    public string OptionId { get; set; } = null!;
    public string MenuId { get; set; } = null!;
    public string OptionName { get; set; } = null!;
    public decimal ExtraPrice { get; set; }

    public virtual FoodMenu Menu { get; set; } = null!;
}
