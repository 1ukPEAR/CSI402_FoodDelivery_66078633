using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using WebApplication1.FoodViewModels;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers
{
    public class CartController : Controller
    {
        private readonly FooddeliverydbContext _db;

        public CartController(FooddeliverydbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(
            string menuId,
            int quantity = 1,
            List<string>? selectedOptionIds = null,
            string? promotionId = null)
        {
            var menu = _db.FoodMenus.FirstOrDefault(m => m.MenuId == menuId);

            if (menu == null)
            {
                TempData["CartMessage"] = "ไม่พบเมนูที่ต้องการเพิ่ม";
                return RedirectToAction("MenuUser", "Menu");
            }
            if (quantity <= 0)
            {
                quantity = 1;
            }

            // ==============================
            // STEP 1: ดึง option ที่เลือก (เฉพาะของเมนูนี้)
            // ==============================
            var selectedIds = selectedOptionIds ?? new List<string>();

            var selectedOptions = _db.FoodMenuoptions
                .Where(o => selectedIds.Contains(o.OptionId) && o.MenuId == menuId)
                .OrderBy(o => o.OptionName)
                .ToList();

            decimal extraPriceTotal = selectedOptions.Sum(o => o.ExtraPrice);

            var selectedOptionNames = selectedOptions
                .Select(o => o.OptionName ?? "")
                .ToList();

            // ใช้สำหรับเซฟลง FoodOrderitem.SelectedOptions ในภายหลัง
            var selectedOptionDisplayNames = selectedOptions
                .Select(o => $"{(o.OptionName ?? "")} (+{o.ExtraPrice:0.##})")
                .ToList();

            var cart = GetCart();

            // ==============================
            // STEP 2: เตรียมข้อมูลโปรโมชั่น
            // ==============================
            string? appliedPromotionId = null;
            string? appliedPromotionName = null;
            string? discountType = null;
            decimal discountValue = 0m;
            decimal discountPercent = 0m;

            if (!string.IsNullOrWhiteSpace(promotionId))
            {
                var now = DateTime.Now;

                var promotion = _db.FoodPromotions.FirstOrDefault(p => p.PromotionId == promotionId);

                if (promotion == null)
                {
                    TempData["CartMessage"] = "ไม่พบโปรโมชั่นที่เลือก";
                    return RedirectToAction("MenuUser", "Menu");
                }

                bool isDateTimeValid =
                    (!promotion.StartDate.HasValue || promotion.StartDate.Value <= now) &&
                    (!promotion.EndDate.HasValue || promotion.EndDate.Value >= now);

                if (!isDateTimeValid)
                {
                    TempData["CartMessage"] = $"โปรโมชั่น \"{promotion.PromotionName}\" หมดอายุหรือยังไม่เริ่มใช้งาน";
                    return RedirectToAction("MenuUser", "Menu");
                }

                bool menuInPromotion = _db.FoodPromotionMenus.Any(pm =>
                    pm.PromotionId == promotionId &&
                    pm.MenuId == menuId);

                if (!menuInPromotion)
                {
                    TempData["CartMessage"] = $"เมนู \"{menu.MenuName}\" ไม่ได้อยู่ในโปรโมชั่นนี้";
                    return RedirectToAction("MenuUser", "Menu");
                }

                string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = HttpContext.Session.GetString("UserId");
                }

                // =====================================================
                // milestone promotion check
                // =====================================================
                if (promotion.IsMilestonePromotion)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        TempData["CartMessage"] = "กรุณาเข้าสู่ระบบก่อนใช้โปรโมชั่นนี้";
                        return RedirectToAction("MenuUser", "Menu");
                    }

                    if (!CanUseMilestonePromotion(userId, promotion, cart))
                    {
                        int requiredCount = promotion.RequiredOrderCount ?? 0;
                        TempData["CartMessage"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้เมื่อมีออเดอร์สำเร็จครบทุก {requiredCount} ครั้ง";
                        return RedirectToAction("MenuUser", "Menu");
                    }

                    if (quantity > 1)
                    {
                        TempData["CartMessage"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้ครั้งละ 1 รายการต่อสิทธิ์";
                        return RedirectToAction("MenuUser", "Menu");
                    }
                }

                // จำกัดต่อผู้ใช้
                if (promotion.MaxUsePerUser.HasValue && promotion.MaxUsePerUser.Value > 0)
                {
                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        TempData["CartMessage"] = "กรุณาเข้าสู่ระบบก่อนใช้โปรโมชั่นนี้";
                        return RedirectToAction("MenuUser", "Menu");
                    }

                    int usedByUser = _db.FoodPromotionUsages
                        .Where(x => x.PromotionId == promotionId && x.UserId == userId)
                        .Sum(x => (int?)x.UsedQty) ?? 0;

                    int qtyInCartSamePromotion = cart
                        .Where(x => x.PromotionId == promotionId)
                        .Sum(x => x.Quantity);

                    if (promotion.IsMilestonePromotion)
                    {
                        int reservedMilestoneInCart = cart.Count(x => x.PromotionId == promotionId);
                        int currentUsage = 1;
                        int totalWouldBe = usedByUser + reservedMilestoneInCart + currentUsage;

                        if (totalWouldBe > promotion.MaxUsePerUser.Value)
                        {
                            TempData["CartMessage"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้สูงสุด {promotion.MaxUsePerUser.Value} ครั้งต่อผู้ใช้";
                            return RedirectToAction("MenuUser", "Menu");
                        }
                    }
                    else
                    {
                        int currentUsage = quantity;
                        int totalWouldBe = usedByUser + qtyInCartSamePromotion + currentUsage;

                        if (totalWouldBe > promotion.MaxUsePerUser.Value)
                        {
                            TempData["CartMessage"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้สูงสุด {promotion.MaxUsePerUser.Value} ครั้งต่อผู้ใช้";
                            return RedirectToAction("MenuUser", "Menu");
                        }
                    }
                }

                // จำกัดต่อออเดอร์
                if (promotion.MaxUsePerOrder.HasValue && promotion.MaxUsePerOrder.Value > 0)
                {
                    if (promotion.IsMilestonePromotion)
                    {
                        int reservedMilestoneInCart = cart.Count(x => x.PromotionId == promotionId);
                        int totalWouldBe = reservedMilestoneInCart + 1;

                        if (totalWouldBe > promotion.MaxUsePerOrder.Value)
                        {
                            TempData["CartMessage"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้สูงสุด {promotion.MaxUsePerOrder.Value} ครั้งต่อออเดอร์";
                            return RedirectToAction("MenuUser", "Menu");
                        }
                    }
                    else
                    {
                        int qtyInCartSamePromotion = cart
                            .Where(x => x.PromotionId == promotionId)
                            .Sum(x => x.Quantity);

                        int totalWouldBe = qtyInCartSamePromotion + quantity;

                        if (totalWouldBe > promotion.MaxUsePerOrder.Value)
                        {
                            TempData["CartMessage"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้สูงสุด {promotion.MaxUsePerOrder.Value} รายการต่อออเดอร์";
                            return RedirectToAction("MenuUser", "Menu");
                        }
                    }
                }

                appliedPromotionId = promotion.PromotionId;
                appliedPromotionName = promotion.PromotionName;

                discountType = string.IsNullOrWhiteSpace(promotion.DiscountType)
                    ? "PERCENT"
                    : promotion.DiscountType.Trim().ToUpper();

                discountValue = promotion.DiscountValue > 0
                    ? promotion.DiscountValue
                    : (decimal)(promotion.DiscountPercent ?? 0);

                discountPercent = discountType == "PERCENT"
                    ? discountValue
                    : 0m;
            }

            // ==============================
            // STEP 3: หา item เดิมในตะกร้า (menu + promo + option set)
            // ==============================
            var sortedSelectedIds = selectedOptions
                .Select(o => o.OptionId)
                .OrderBy(x => x)
                .ToList();

            var newOptionKey = sortedSelectedIds.Any()
                ? string.Join("|", sortedSelectedIds)
                : "NO_OPTION";

            var existingItem = cart.FirstOrDefault(c =>
                c.MenuId == menuId &&
                (c.PromotionId ?? "") == (appliedPromotionId ?? "") &&
                GetSafeOptionKey(c) == newOptionKey
            );

            if (existingItem != null)
            {
                // ถ้าเป็นรายการโปร -> ห้ามเพิ่มจำนวน
                if (existingItem.HasPromotion)
                {
                    TempData["CartMessage"] = "รายการโปรโมชั่นไม่สามารถเพิ่มจำนวนได้ กรุณาใช้ปุ่มลบแล้วเลือกใหม่";
                    return RedirectToAction("MenuUser", "Menu");
                }

                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    MenuId = menu.MenuId,
                    MenuName = menu.MenuName ?? "",
                    Price = menu.MenuPrice ?? 0,

                    MenuImage = string.IsNullOrWhiteSpace(menu.MenuImage)
                        ? "/img/menu/default-food.jpg"
                        : (menu.MenuImage.StartsWith("/") ? menu.MenuImage : "/img/menu/" + menu.MenuImage),

                    Quantity = quantity,

                    SelectedOptionIds = sortedSelectedIds,
                    SelectedOptionNames = selectedOptionNames,
                    SelectedOptionDisplayNames = selectedOptionDisplayNames,
                    ExtraPriceTotal = extraPriceTotal,

                    PromotionId = appliedPromotionId,
                    PromotionName = appliedPromotionName,
                    DiscountType = discountType,
                    DiscountValue = discountValue,
                    DiscountPercent = discountPercent
                });
            }

            SaveCart(cart);

            if (!string.IsNullOrWhiteSpace(appliedPromotionId))
            {
                TempData["CartMessage"] = $"เพิ่ม \"{menu.MenuName}\" พร้อมใช้โปรโมชั่น \"{appliedPromotionName}\" ลงตะกร้าแล้ว";
            }
            else
            {
                TempData["CartMessage"] = $"เพิ่ม \"{menu.MenuName}\" ลงตะกร้าแล้ว";
            }

            return RedirectToAction("MenuUser", "Menu");
        }

        [HttpGet]
        public IActionResult CartPopup()
        {
            var cart = GetCart();
            return PartialView("CartPopup", cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IncreaseQuantity(int index)
        {
            var cart = GetCart();

            if (index >= 0 && index < cart.Count)
            {
                var item = cart[index];

                if (item.HasPromotion)
                {
                    TempData["CartMessage"] = "รายการโปรโมชั่นไม่สามารถเพิ่มจำนวนได้ กรุณาใช้ปุ่มลบแล้วเลือกใหม่";
                    return RedirectToAction("MenuUser", "Menu");
                }

                cart[index].Quantity++;
                SaveCart(cart);
            }

            return RedirectToAction("MenuUser", "Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DecreaseQuantity(int index)
        {
            var cart = GetCart();

            if (index >= 0 && index < cart.Count)
            {
                var item = cart[index];

                if (item.HasPromotion)
                {
                    TempData["CartMessage"] = "รายการโปรโมชั่นไม่สามารถลดจำนวนได้ กรุณาใช้ปุ่มลบแทน";
                    return RedirectToAction("MenuUser", "Menu");
                }

                if (cart[index].Quantity > 1)
                {
                    cart[index].Quantity--;
                }
                else
                {
                    cart.RemoveAt(index);
                }

                SaveCart(cart);
            }

            return RedirectToAction("MenuUser", "Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int index)
        {
            var cart = GetCart();

            if (index >= 0 && index < cart.Count)
            {
                cart.RemoveAt(index);
                SaveCart(cart);
            }

            return RedirectToAction("MenuUser", "Menu");
        }

        [HttpGet]
        public IActionResult CheckoutOld()
        {
            var cart = GetCart();

            if (!cart.Any())
            {
                TempData["CartMessage"] = "ยังไม่มีสินค้าในตะกร้า";
                return RedirectToAction("MenuUser", "Menu");
            }

            return View("Checkout", cart);
        }

        // =========================
        // Helper Methods
        // =========================
        private List<CartItemViewModel> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");

            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItemViewModel>()
                : JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson) ?? new List<CartItemViewModel>();

            // กันข้อมูลเก่าใน Session ที่ยังไม่มี field ใหม่
            foreach (var item in cart)
            {
                item.SelectedOptionIds ??= new List<string>();
                item.SelectedOptionNames ??= new List<string>();
                item.SelectedOptionDisplayNames ??= new List<string>();
            }

            return cart;
        }

        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        private string GetSafeOptionKey(CartItemViewModel item)
        {
            if (item.SelectedOptionIds == null || !item.SelectedOptionIds.Any())
                return "NO_OPTION";

            return string.Join("|", item.SelectedOptionIds.OrderBy(x => x));
        }

        // =====================================================
        // HELPER : milestone promotion
        // =====================================================
        private bool CanUseMilestonePromotion(string userId, FoodPromotion promotion, List<CartItemViewModel> cart)
        {
            if (string.IsNullOrWhiteSpace(userId) || promotion == null) return false;
            if (!promotion.IsMilestonePromotion) return true;
            if (!promotion.RequiredOrderCount.HasValue || promotion.RequiredOrderCount.Value <= 0) return false;

            int requiredCount = promotion.RequiredOrderCount.Value;

            int successOrderCount = _db.FoodOrders.Count(o =>
                o.UserId == userId &&
                o.OrderStatus == 7UL);

            int entitledUses = successOrderCount / requiredCount;

            if (entitledUses <= 0) return false;

            int usedCount = _db.FoodPromotionUsages
                .Where(u => u.UserId == userId && u.PromotionId == promotion.PromotionId)
                .Sum(u => (int?)u.UsedQty) ?? 0;

            int reservedInCart = cart.Count(x => x.PromotionId == promotion.PromotionId);

            return (usedCount + reservedInCart) < entitledUses;
        }
    }
}