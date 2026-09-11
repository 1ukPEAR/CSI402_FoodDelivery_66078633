using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodMenutype
{
    public string MenuTypeId { get; set; } = null!;
    public string MenuTypeName { get; set; } = null!;

    public virtual ICollection<FoodMenu> FoodMenus { get; set; } = new List<FoodMenu>();
}
