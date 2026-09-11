using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodOrderChatRoom
{
    public string ChatRoomId { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    
    public virtual FoodOrder Order { get; set; } = null!;
    public virtual ICollection<FoodOrderChatMessage> FoodOrderChatMessages { get; set; } = new List<FoodOrderChatMessage>();
}