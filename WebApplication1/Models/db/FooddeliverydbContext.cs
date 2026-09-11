using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace WebApplication1.Models.db;

public partial class FooddeliverydbContext : DbContext
{
    public FooddeliverydbContext()
    {
    }

    public FooddeliverydbContext(DbContextOptions<FooddeliverydbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<FoodAddress> FoodAddresses { get; set; }

    public virtual DbSet<FoodMenu> FoodMenus { get; set; }

    public virtual DbSet<FoodMenuoption> FoodMenuoptions { get; set; }

    public virtual DbSet<FoodMenutype> FoodMenutypes { get; set; }

    public virtual DbSet<FoodOrder> FoodOrders { get; set; }

    public virtual DbSet<FoodOrderitem> FoodOrderitems { get; set; }

    public virtual DbSet<FoodPromotion> FoodPromotions { get; set; }
    public virtual DbSet<FoodPromotionMenu> FoodPromotionMenus { get; set; }

    public virtual DbSet<FoodReview> FoodReviews { get; set; }

    public virtual DbSet<FoodRole> FoodRoles { get; set; }

    public virtual DbSet<FoodUser> FoodUsers { get; set; }

    public virtual DbSet<FoodPromotionUsage> FoodPromotionUsages { get; set; }

    public virtual DbSet<FoodOrderChatRoom> FoodOrderChatRooms { get; set; }

    public virtual DbSet<FoodOrderChatMessage> FoodOrderChatMessages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("server=localhost;port=3305;database=fooddeliverydb;user=root;password=Pairry121819", Microsoft.EntityFrameworkCore.ServerVersion.Parse("9.6.0-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<FoodAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PRIMARY");

            entity.ToTable("food_address");

            entity.HasIndex(e => e.UserId, "FK_Address_User");

            entity.Property(e => e.AddressId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.AddressDetail)
                .HasMaxLength(255)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.Province)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.ReceiverPhone)
                .HasMaxLength(15)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.SubDistrict)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.UserId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.HasOne(d => d.User).WithMany(p => p.FoodAddresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Address_User");
        });

        modelBuilder.Entity<FoodOrderChatRoom>(entity =>
{
    entity.HasKey(e => e.ChatRoomId).HasName("PRIMARY");

    entity.ToTable("food_order_chat_room");

    entity.HasIndex(e => e.OrderId, "FK_OrderChatRoom_Order");

    entity.Property(e => e.ChatRoomId)
        .HasMaxLength(10)
        .UseCollation("utf8mb3_general_ci")
        .HasCharSet("utf8mb3");

    entity.Property(e => e.OrderId)
        .HasMaxLength(50)
        .UseCollation("utf8mb3_general_ci")
        .HasCharSet("utf8mb3");

    entity.Property(e => e.CreatedAt).HasColumnType("datetime");

    entity.HasOne(d => d.Order)
        .WithMany(p => p.FoodOrderChatRooms)
        .HasForeignKey(d => d.OrderId)
        .OnDelete(DeleteBehavior.Cascade)
        .HasConstraintName("FK_OrderChatRoom_Order");
});
        modelBuilder.Entity<FoodOrderChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PRIMARY");

            entity.ToTable("food_order_chat_message");

            entity.HasIndex(e => e.ChatRoomId, "FK_OrderChatMessage_ChatRoom");
            entity.HasIndex(e => e.SenderUserId, "FK_OrderChatMessage_User");

            entity.Property(e => e.MessageId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ChatRoomId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.SenderUserId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.MessageText)
                .HasColumnType("text")
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.SentAt).HasColumnType("datetime");

            entity.Property(e => e.IsRead)
                .HasDefaultValueSql("'0'");

            entity.HasOne(d => d.ChatRoom)
                .WithMany(p => p.FoodOrderChatMessages)
                .HasForeignKey(d => d.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrderChatMessage_ChatRoom");

            entity.HasOne(d => d.SenderUser)
                .WithMany(p => p.FoodOrderChatMessages)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderChatMessage_User");
        });

        modelBuilder.Entity<FoodMenu>(entity =>
        {
            entity.HasKey(e => e.MenuId).HasName("PRIMARY");

            entity.ToTable("food_menu");

            entity.HasIndex(e => e.MenuTypeId, "FK_Menu_Type");

            entity.Property(e => e.MenuId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.MenuName)
                .HasMaxLength(200)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.MenuPrice)
                .HasPrecision(10, 2);

            entity.Property(e => e.MenuTypeId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            // เพิ่มตรงนี้สำหรับสถานะ
            entity.Property(e => e.MenuStatus)
                .HasDefaultValueSql("b'1'")
                .HasColumnType("bit(1)");

            // รูปภาพ
            entity.Property(e => e.MenuImage)
                .HasMaxLength(255)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.HasOne(d => d.MenuType)
                .WithMany(p => p.FoodMenus)
                .HasForeignKey(d => d.MenuTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Menu_Type");
        });

        modelBuilder.Entity<FoodMenuoption>(entity =>
        {
            entity.HasKey(e => e.OptionId).HasName("PRIMARY");

            entity.ToTable("food_menuoption");

            entity.HasIndex(e => e.MenuId, "FK_MenuOption_Menu");

            entity.Property(e => e.OptionId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.ExtraPrice).HasPrecision(10, 2);
            entity.Property(e => e.MenuId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.OptionName)
                .HasMaxLength(200)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.HasOne(d => d.Menu).WithMany(p => p.FoodMenuoptions)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MenuOption_Menu");
        });

        modelBuilder.Entity<FoodMenutype>(entity =>
        {
            entity.HasKey(e => e.MenuTypeId).HasName("PRIMARY");

            entity.ToTable("food_menutype");

            entity.Property(e => e.MenuTypeId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.MenuTypeName)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
        });

        modelBuilder.Entity<FoodOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PRIMARY");

            entity.ToTable("food_order");

            entity.HasIndex(e => e.PromotionId, "FK_Order_Promotion");
            entity.HasIndex(e => e.UserId, "FK_Order_User");

            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.UserId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.PromotionId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ReviewDecision)
                .HasColumnType("tinyint");

            entity.Property(e => e.ReviewDecidedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2);

            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2);

            entity.Property(e => e.FinalPrice)
                .HasPrecision(10, 2);

            entity.Property(e => e.OrderStatus);

            entity.Property(e => e.HandledByUserId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.PaymentSlip)
                .HasMaxLength(255)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.OrderNote)
                .HasMaxLength(500)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ShippingAddressId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ShippingReceiverName)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ShippingPhone)
                .HasMaxLength(15)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ShippingAddressDetail)
                .HasMaxLength(255)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ShippingSubDistrict)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ShippingDistrict)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ShippingProvince)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.PointsEarned);

            entity.HasOne(d => d.User)
                .WithMany(p => p.FoodOrders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Order_User");

            entity.HasOne(d => d.Promotion)
                .WithMany(p => p.FoodOrders)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_Order_Promotion");
        });

        modelBuilder.Entity<FoodOrderitem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PRIMARY");

            entity.ToTable("food_orderitem");

            entity.HasIndex(e => e.MenuId, "FK_OrderItem_Menu");

            entity.HasIndex(e => e.OrderId, "FK_OrderItem_Order");

            entity.Property(e => e.OrderItemId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.MenuId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.OrderId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.SelectedOptions)
                .HasColumnType("text")
                .HasColumnName("selected_options");

            entity.Property(e => e.ExtraOptionPrice)
                .HasPrecision(10, 2)
                .HasColumnName("extra_option_price");
            entity.Property(e => e.Price).HasPrecision(10, 2);

            entity.HasOne(d => d.Menu).WithMany(p => p.FoodOrderitems)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Menu");

            entity.HasOne(d => d.Order).WithMany(p => p.FoodOrderitems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItem_Order");
        });

        modelBuilder.Entity<FoodPromotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PRIMARY");

            entity.ToTable("food_promotion");

            entity.Property(e => e.PromotionId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.PromotionName)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.DescriptionPro)
                .HasMaxLength(255)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.DiscountPercent);

            // ของเดิม
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PERCENT'")
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.DiscountValue)
                .HasPrecision(10, 2)
                .HasDefaultValue(0m);

            entity.Property(e => e.MaxUsePerUser)
                .HasColumnType("int");

            entity.Property(e => e.MaxUsePerOrder)
                .HasColumnType("int");

           entity.Property(e => e.IsMilestonePromotion)
                .HasDefaultValue(false);

            entity.Property(e => e.RequiredOrderCount)
                .HasColumnType("int");

            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<FoodPromotionMenu>(entity =>
        {
            entity.HasKey(e => new { e.PromotionId, e.MenuId }).HasName("PRIMARY");

            entity.ToTable("food_promotion_menu");

            entity.HasIndex(e => e.MenuId, "FK_PM_Menu");

            entity.Property(e => e.PromotionId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.MenuId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.HasOne(d => d.Promotion)
                .WithMany(p => p.FoodPromotionMenus)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PM_Promotion");

            entity.HasOne(d => d.Menu)
                .WithMany(p => p.FoodPromotionMenus)
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PM_Menu");
        });

        modelBuilder.Entity<FoodPromotionUsage>(entity =>
        {
            entity.HasKey(e => e.UsageId).HasName("PRIMARY");

            entity.ToTable("food_promotion_usage");

            entity.HasIndex(e => e.PromotionId, "FK_FoodPromotionUsage_Promotion");
            entity.HasIndex(e => e.UserId, "FK_FoodPromotionUsage_User");
            entity.HasIndex(e => e.OrderId, "FK_FoodPromotionUsage_Order");

            entity.Property(e => e.UsageId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.PromotionId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.UserId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.UsedAt).HasColumnType("datetime");

            entity.Property(e => e.UsedQty)
                .HasDefaultValue(1);

            entity.HasOne(d => d.Promotion)
                .WithMany(p => p.FoodPromotionUsages)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FoodPromotionUsage_Promotion");

            entity.HasOne(d => d.User)
                .WithMany(p => p.FoodPromotionUsages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FoodPromotionUsage_User");

            entity.HasOne(d => d.Order)
                .WithMany(p => p.FoodPromotionUsages)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_FoodPromotionUsage_Order");
        });

        modelBuilder.Entity<FoodReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PRIMARY");

            entity.ToTable("food_review");

            entity.HasIndex(e => e.UserId, "FK_Review_User");

            entity.Property(e => e.ReviewId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.Score);

            entity.Property(e => e.CommentReview)
                .HasMaxLength(500)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.Property(e => e.ReviewDate)
                .HasColumnType("datetime");

            entity.Property(e => e.UserId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");

            entity.HasOne(d => d.UserReview)
                .WithMany(p => p.FoodReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Review_User");
        });

        modelBuilder.Entity<FoodRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");

            entity.ToTable("food_role");

            entity.Property(e => e.RoleId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.RoleStatus)
                .HasDefaultValueSql("b'1'")
                .HasColumnType("bit(1)");
        });

        modelBuilder.Entity<FoodUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("food_user");

            entity.HasIndex(e => e.RoleId, "FK_User_Role");

            entity.Property(e => e.UserId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.RegisterDate).HasColumnType("datetime");
            entity.Property(e => e.RoleId)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(100)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.UserName)
                .HasMaxLength(30)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.UserPassword)
                .HasMaxLength(255)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.UserPhone)
                .HasMaxLength(10)
                .UseCollation("utf8mb3_general_ci")
                .HasCharSet("utf8mb3");
            entity.Property(e => e.UserStatus)
                .HasDefaultValueSql("b'1'")
                .HasColumnType("bit(1)");
            entity.Property(e => e.LoyaltyPoints)
                .HasDefaultValueSql("'0'");
            entity.Property(e => e.OrderCount)
                .HasDefaultValueSql("'0'");

            entity.HasOne(d => d.Role).WithMany(p => p.FoodUsers)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_User_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
