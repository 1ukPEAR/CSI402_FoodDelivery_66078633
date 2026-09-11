using System;
using System.Collections.Generic;

namespace WebApplication1.Models.db;

public partial class FoodOrder
{
    public string OrderId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateOnly OrderDate { get; set; }
    public TimeOnly OrderTime { get; set; }
    public decimal TotalPrice { get; set; }
    public string? PromotionId { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public ulong? OrderStatus { get; set; }
    public string? HandledByUserId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentSlip { get; set; }
    public string? OrderNote { get; set; }
    public byte? ReviewDecision { get; set; }
    public DateTime? ReviewDecidedAt { get; set; }
    public int? PointsEarned { get; set; }
    public string? ShippingAddressId { get; set; }
    public string? ShippingReceiverName { get; set; }
    public string? ShippingPhone { get; set; }
    public string? ShippingAddressDetail { get; set; }
    public string? ShippingSubDistrict { get; set; }
    public string? ShippingDistrict { get; set; }
    public string? ShippingProvince { get; set; }

    public virtual ICollection<FoodOrderitem> FoodOrderitems { get; set; } = new List<FoodOrderitem>();
    public virtual ICollection<FoodPromotionUsage> FoodPromotionUsages { get; set; } = new List<FoodPromotionUsage>();
    public virtual ICollection<FoodOrderChatRoom> FoodOrderChatRooms { get; set; } = new List<FoodOrderChatRoom>();
    public virtual FoodPromotion? Promotion { get; set; }
    public virtual FoodUser User { get; set; } = null!;
}