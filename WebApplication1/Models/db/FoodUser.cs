using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.db;

public partial class FoodUser
{
    public string UserId { get; set; } = null!;
    public string? RoleId { get; set; }
    public string UserName { get; set; } = null!;
    public string UserPassword { get; set; } = null!;
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public ulong? UserStatus { get; set; }
    public DateTime? RegisterDate { get; set; }
    public int? LoyaltyPoints { get; set; }
    public int? OrderCount { get; set; }
    [NotMapped]
    public string? NewPassword { get; set; }
    [NotMapped]
    public string? ConfirmPassword { get; set; }

    public virtual ICollection<FoodAddress> FoodAddresses { get; set; } = new List<FoodAddress>();
    public virtual ICollection<FoodOrder> FoodOrders { get; set; } = new List<FoodOrder>();
    public virtual ICollection<FoodPromotionUsage> FoodPromotionUsages { get; set; } = new List<FoodPromotionUsage>();
    public virtual ICollection<FoodReview> FoodReviews { get; set; } = new List<FoodReview>();
    public virtual ICollection<FoodOrderChatMessage> FoodOrderChatMessages { get; set; } = new List<FoodOrderChatMessage>();
    public virtual FoodRole? Role { get; set; }
}