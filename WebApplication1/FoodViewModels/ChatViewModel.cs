using System;
using System.Collections.Generic;

namespace WebApplication1.FoodViewModels
{
    public class ChatViewModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string ChatRoomId { get; set; } = string.Empty;
        public bool CanSendMessage { get; set; }
        public bool IsCustomerOwner { get; set; }
        public string CustomerUserId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public ulong? OrderStatus { get; set; }
        public List<ChatMessageItemViewModel> Messages { get; set; } = new();
    }

    public class ChatMessageItemViewModel
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderUserId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsMine { get; set; }
    }
}