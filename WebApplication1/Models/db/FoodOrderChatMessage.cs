using System;

namespace WebApplication1.Models.db;

public partial class FoodOrderChatMessage
{
    public string MessageId { get; set; } = null!;
    public string ChatRoomId { get; set; } = null!;
    public string SenderUserId { get; set; } = null!;
    public string MessageText { get; set; } = null!;
    public DateTime SentAt { get; set; }
    public ulong? IsRead { get; set; }

    public virtual FoodOrderChatRoom ChatRoom { get; set; } = null!;
    public virtual FoodUser SenderUser { get; set; } = null!;
}