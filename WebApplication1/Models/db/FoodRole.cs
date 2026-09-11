using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodRole
{
    public string RoleId { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public ulong? RoleStatus { get; set; }

    public virtual ICollection<FoodUser> FoodUsers { get; set; } = new List<FoodUser>();
}
