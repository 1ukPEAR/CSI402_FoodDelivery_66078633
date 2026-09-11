using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodAddress
{
    public string AddressId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string ReceiverPhone { get; set; } = null!;
    public string AddressDetail { get; set; } = null!;
    public string SubDistrict { get; set; } = null!;
    public string District { get; set; } = null!;
    public string Province { get; set; } = null!;
    public virtual FoodUser User { get; set; } = null!;
}