using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.FoodViewModels;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly FooddeliverydbContext _db;

        public OrderController(IWebHostEnvironment env, FooddeliverydbContext db)
        {
            _env = env;
            _db = db;
        }

        // EMPLOYEE + OWNER : ดูรายการออเดอร์ทั้งหมด
        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpGet]
        public IActionResult OrderList()
        {
            var orders = _db.FoodOrders
                .Include(o => o.Promotion)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .ToList();

            ViewBag.OrderItems = _db.FoodOrderitems.ToList();
            ViewBag.AllMenus = _db.FoodMenus.ToList();
            ViewBag.AllUsers = _db.FoodUsers.ToList();

            return View(orders);
        }

        // OWNER : ประวัติทั้งหมด
        [Authorize(Roles = "OWNER")]
        [HttpGet]
        public IActionResult HistoryOwner()
        {
            var orders = _db.FoodOrders
                .Include(o => o.Promotion)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .ToList();

            ViewBag.OrderItems = _db.FoodOrderitems.ToList();
            ViewBag.AllMenus = _db.FoodMenus.ToList();
            ViewBag.AllUsers = _db.FoodUsers.ToList();

            return View(orders);
        }

        // CUSTOMER : ประวัติตัวเอง + แต้มสะสม
        [Authorize(Roles = "CUSTOMER")]
        [HttpGet]
        public IActionResult HistoryUser()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var orders = _db.FoodOrders
                .Include(o => o.Promotion)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .ToList();

            ViewBag.OrderItems = _db.FoodOrderitems.ToList();
            ViewBag.AllMenus = _db.FoodMenus.ToList();

            var user = _db.FoodUsers.FirstOrDefault(u => u.UserId == userId);
            ViewBag.CurrentUser = user;

            return View(orders);
        }

        // EMPLOYEE : ประวัติการขายของตัวเอง
        [Authorize(Roles = "EMPLOYEE")]
        [HttpGet]
        public IActionResult HistoryEmployee()
        {
            string? employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งาน";
                return RedirectToAction("Login", "Auth");
            }

            var orders = _db.FoodOrders
                .Include(o => o.Promotion)
                .Where(o => o.HandledByUserId == employeeId && o.OrderStatus == 7UL)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .ToList();

            ViewBag.OrderItems = _db.FoodOrderitems.ToList();
            ViewBag.AllMenus = _db.FoodMenus.ToList();
            ViewBag.AllUsers = _db.FoodUsers.ToList();

            var currentEmployee = _db.FoodUsers.FirstOrDefault(u => u.UserId == employeeId);
            ViewBag.CurrentEmployee = currentEmployee;

            return View(orders);
        }

        // ORDER STATUS (CUSTOMER)
        [Authorize(Roles = "CUSTOMER")]
        [HttpGet]
        public IActionResult OrderStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "กรุณาเข้าสู่ระบบก่อน";
                return RedirectToAction("Login", "Auth");
            }

            var orders = _db.FoodOrders
                .Include(o => o.Promotion)
                .Where(o => o.UserId == userId)
                .Where(o => o.OrderStatus != 8UL)
                .Where(o => o.OrderStatus != 7UL || o.ReviewDecision == null)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .ToList();

            var orderIds = orders.Select(o => o.OrderId).ToList();

            ViewBag.OrderItems = _db.FoodOrderitems
                .Where(oi => orderIds.Contains(oi.OrderId))
                .ToList();

            ViewBag.AllMenus = _db.FoodMenus
                .ToList();

            ViewBag.CurrentUser = _db.FoodUsers
                .FirstOrDefault(u => u.UserId == userId);

            ViewBag.Reviews = _db.FoodReviews
                .Where(r => orderIds.Contains(r.OrderId))
                .ToList();

            return View(orders);
        }

        // CUSTOMER : หน้า Checkout ก่อนยืนยันสั่งซื้อ
        [Authorize(Roles = "CUSTOMER")]
        [HttpGet]
        public IActionResult Checkout()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งาน";
                return RedirectToAction("Login", "Auth");
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                TempData["Error"] = "ไม่มีสินค้าในตะกร้า";
                return RedirectToAction("MenuUser", "Menu");
            }

            var cartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);
            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "ไม่มีสินค้าในตะกร้า";
                return RedirectToAction("MenuUser", "Menu");
            }

            var addresses = _db.FoodAddresses
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.AddressId)
                .ToList();

            decimal totalPrice = cartItems.Sum(x => x.OriginalTotalPrice);
            decimal discountAmount = cartItems.Sum(x => x.TotalDiscountAmount);
            decimal finalPrice = cartItems.Sum(x => x.TotalPrice);

            var vm = new CheckoutViewModel
            {
                CartItems = cartItems,
                Addresses = addresses,
                TotalPrice = totalPrice,
                DiscountAmount = discountAmount,
                FinalPrice = finalPrice
            };

            return View(vm);
        }

        // CUSTOMER : ลูกค้ากดยืนยันคำสั่งซื้อ
        // 0 -> 1
        [Authorize(Roles = "CUSTOMER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(PlaceOrderViewModel vm)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งาน";
                return RedirectToAction("Login", "Auth");
            }

            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                TempData["Error"] = "ไม่มีสินค้าในตะกร้า";
                return RedirectToAction("MenuUser", "Menu");
            }

            var cartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartJson);
            if (cartItems == null || !cartItems.Any())
            {
                TempData["Error"] = "ไม่มีสินค้าในตะกร้า";
                return RedirectToAction("MenuUser", "Menu");
            }

            // เตรียมข้อมูลที่อยู่จัดส่ง
            string? shippingAddressId = null;
            string? shippingReceiverName = null;
            string? shippingPhone = null;
            string? shippingAddressDetail = null;
            string? shippingSubDistrict = null;
            string? shippingDistrict = null;
            string? shippingProvince = null;

            if (vm.UseNewAddress)
            {
                if (string.IsNullOrWhiteSpace(vm.NewReceiverName) ||
                    string.IsNullOrWhiteSpace(vm.NewReceiverPhone) ||
                    string.IsNullOrWhiteSpace(vm.NewAddressDetail) ||
                    string.IsNullOrWhiteSpace(vm.NewSubDistrict) ||
                    string.IsNullOrWhiteSpace(vm.NewDistrict) ||
                    string.IsNullOrWhiteSpace(vm.NewProvince))
                {
                    TempData["Error"] = "กรุณากรอกข้อมูลที่อยู่ใหม่ให้ครบ";
                    return RedirectToAction("Checkout");
                }

                shippingReceiverName = vm.NewReceiverName.Trim();
                shippingPhone = vm.NewReceiverPhone.Trim();
                shippingAddressDetail = vm.NewAddressDetail.Trim();
                shippingSubDistrict = vm.NewSubDistrict.Trim();
                shippingDistrict = vm.NewDistrict.Trim();
                shippingProvince = vm.NewProvince.Trim();

                if (vm.SaveNewAddressToBook)
                {
                    string newAddressId = GenerateNextAddressId();

                    var newAddress = new FoodAddress
                    {
                        AddressId = newAddressId,
                        UserId = userId,
                        UserName = shippingReceiverName,
                        ReceiverPhone = shippingPhone,
                        AddressDetail = shippingAddressDetail,
                        SubDistrict = shippingSubDistrict,
                        District = shippingDistrict,
                        Province = shippingProvince
                    };

                    _db.FoodAddresses.Add(newAddress);
                    shippingAddressId = newAddressId;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vm.SelectedAddressId))
                {
                    TempData["Error"] = "กรุณาเลือกที่อยู่จัดส่ง";
                    return RedirectToAction("Checkout");
                }

                var selectedAddress = _db.FoodAddresses
                    .FirstOrDefault(a => a.AddressId == vm.SelectedAddressId && a.UserId == userId);

                if (selectedAddress == null)
                {
                    TempData["Error"] = "ไม่พบที่อยู่ที่เลือก";
                    return RedirectToAction("Checkout");
                }

                shippingAddressId = selectedAddress.AddressId;
                shippingReceiverName = selectedAddress.UserName;
                shippingPhone = selectedAddress.ReceiverPhone;
                shippingAddressDetail = selectedAddress.AddressDetail;
                shippingSubDistrict = selectedAddress.SubDistrict;
                shippingDistrict = selectedAddress.District;
                shippingProvince = selectedAddress.Province;
            }

            var now = DateTime.Now;

        
            // ตรวจสอบโปรโมชั่น + รองรับ milestone
            foreach (var item in cartItems.Where(x => x.HasPromotion))
            {
                var promotion = _db.FoodPromotions.FirstOrDefault(p => p.PromotionId == item.PromotionId);

                if (promotion == null)
                {
                    TempData["Error"] = $"โปรโมชั่นของเมนู \"{item.MenuName}\" ไม่ถูกต้อง";
                    return RedirectToAction("Index", "Cart");
                }

                bool isDateTimeValid =
                    (!promotion.StartDate.HasValue || promotion.StartDate.Value <= now) &&
                    (!promotion.EndDate.HasValue || promotion.EndDate.Value >= now);

                if (!isDateTimeValid)
                {
                    TempData["Error"] = $"โปรโมชั่น \"{promotion.PromotionName}\" หมดอายุหรือยังไม่เริ่มใช้งาน";
                    return RedirectToAction("Index", "Cart");
                }

                bool menuInPromotion = _db.FoodPromotionMenus.Any(pm =>
                    pm.PromotionId == item.PromotionId &&
                    pm.MenuId == item.MenuId);

                if (!menuInPromotion)
                {
                    TempData["Error"] = $"เมนู \"{item.MenuName}\" ไม่ได้อยู่ในโปรโมชั่นแล้ว";
                    return RedirectToAction("Index", "Cart");
                }
            }

            var promoGroups = cartItems
                .Where(x => x.HasPromotion && !string.IsNullOrWhiteSpace(x.PromotionId))
                .GroupBy(x => x.PromotionId)
                .ToList();

            foreach (var group in promoGroups)
            {
                var promotion = _db.FoodPromotions.FirstOrDefault(p => p.PromotionId == group.Key);
                if (promotion == null) continue;

                int qtyInThisOrder = group.Sum(x => x.Quantity);

                // -----------------------------
                // จำกัดต่อออเดอร์
                // milestone = ใช้สิทธิ์ได้ 1 ครั้งต่อ 1 ออเดอร์
                // -----------------------------
                if (promotion.IsMilestonePromotion)
                {
                    if (promotion.MaxUsePerOrder.HasValue && promotion.MaxUsePerOrder.Value > 0)
                    {
                        if (1 > promotion.MaxUsePerOrder.Value)
                        {
                            TempData["Error"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ไม่สามารถใช้ได้ในออเดอร์นี้";
                            return RedirectToAction("Index", "Cart");
                        }
                    }
                }
                else
                {
                    if (promotion.MaxUsePerOrder.HasValue && promotion.MaxUsePerOrder.Value > 0)
                    {
                        if (qtyInThisOrder > promotion.MaxUsePerOrder.Value)
                        {
                            TempData["Error"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้สูงสุด {promotion.MaxUsePerOrder.Value} รายการต่อออเดอร์";
                            return RedirectToAction("Index", "Cart");
                        }
                    }
                }

       
                // จำกัดต่อผู้ใช้
                if (promotion.MaxUsePerUser.HasValue && promotion.MaxUsePerUser.Value > 0)
                {
                    int usedByUser = _db.FoodPromotionUsages
                        .Where(x => x.PromotionId == group.Key && x.UserId == userId)
                        .Sum(x => (int?)x.UsedQty) ?? 0;

                    int currentUsage = promotion.IsMilestonePromotion ? 1 : qtyInThisOrder;

                    if (usedByUser + currentUsage > promotion.MaxUsePerUser.Value)
                    {
                        TempData["Error"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้สูงสุด {promotion.MaxUsePerUser.Value} ครั้งต่อผู้ใช้";
                        return RedirectToAction("Index", "Cart");
                    }
                }

               
                // ต้องมีออเดอร์สำเร็จครบทุก X ครั้งก่อนถึงจะได้ใช้โปรนี้
                if (promotion.IsMilestonePromotion)
                {
                    bool canUseMilestone = CanUseMilestonePromotion(userId, promotion);

                    if (!canUseMilestone)
                    {
                        int requiredCount = promotion.RequiredOrderCount ?? 0;
                        TempData["Error"] = $"โปรโมชั่น \"{promotion.PromotionName}\" ใช้ได้เมื่อมีออเดอร์สำเร็จครบทุก {requiredCount} ครั้ง";
                        return RedirectToAction("Index", "Cart");
                    }
                }
            }

            string newOrderId = GenerateNextOrderId();

            decimal totalPrice = cartItems.Sum(x => x.OriginalTotalPrice);
            decimal discountAmount = cartItems.Sum(x => x.TotalDiscountAmount);
            decimal finalPrice = totalPrice - discountAmount;
            if (finalPrice < 0) finalPrice = 0;

            var distinctPromotionIds = cartItems
                .Where(x => x.HasPromotion && !string.IsNullOrWhiteSpace(x.PromotionId))
                .Select(x => x.PromotionId!)
                .Distinct()
                .ToList();

            string? orderPromotionId = distinctPromotionIds.Count == 1
                ? distinctPromotionIds.First()
                : null;

            var order = new FoodOrder
            {
                OrderId = newOrderId,
                UserId = userId,
                OrderDate = DateOnly.FromDateTime(now),
                OrderTime = TimeOnly.FromDateTime(now),
                TotalPrice = totalPrice,
                PromotionId = orderPromotionId,
                DiscountAmount = discountAmount,
                FinalPrice = finalPrice,
                OrderStatus = 1UL,
                PointsEarned = 0,

                // snapshot ที่อยู่จัดส่ง
                ShippingAddressId = shippingAddressId,
                ShippingReceiverName = shippingReceiverName,
                ShippingPhone = shippingPhone,
                ShippingAddressDetail = shippingAddressDetail,
                ShippingSubDistrict = shippingSubDistrict,
                ShippingDistrict = shippingDistrict,
                ShippingProvince = shippingProvince
            };

            _db.FoodOrders.Add(order);

            int nextOrderItem = GetNextOrderItemNumber();

            foreach (var item in cartItems)
            {
                string newOrderItemId = "OI" + nextOrderItem.ToString("D3");
                nextOrderItem++;

                // ✅ ราคาต่อชิ้นหลังรวม option + หลังหักโปร
                decimal unitPrice = item.FinalPrice;

                // ✅ เก็บ option แบบพร้อมราคา
                string? selectedOptionsText = null;

                if (item.SelectedOptionDisplayNames != null && item.SelectedOptionDisplayNames.Any())
                {
                    selectedOptionsText = string.Join(", ", item.SelectedOptionDisplayNames);
                }
                else if (item.SelectedOptionNames != null && item.SelectedOptionNames.Any())
                {
                    selectedOptionsText = string.Join(", ", item.SelectedOptionNames);
                }

                var orderItem = new FoodOrderitem
                {
                    OrderItemId = newOrderItemId,
                    OrderId = newOrderId,
                    MenuId = item.MenuId,
                    Quantity = item.Quantity,

                
                    Price = unitPrice,
                    SelectedOptions = selectedOptionsText,
                    ExtraOptionPrice = item.ExtraPriceTotal
                };

                _db.FoodOrderitems.Add(orderItem);
            }

           
            _db.SaveChanges();

        
            // บันทึก promotion usage
            foreach (var group in promoGroups)
            {
                var promotion = _db.FoodPromotions.FirstOrDefault(p => p.PromotionId == group.Key);
                if (promotion == null) continue;

                int usedQty = promotion.IsMilestonePromotion ? 1 : group.Sum(x => x.Quantity);

                if (usedQty <= 0) usedQty = 1;

                var usage = new FoodPromotionUsage
                {
                    UsageId = GenerateNextPromotionUsageId(),
                    PromotionId = group.Key!,
                    UserId = userId,
                    OrderId = newOrderId,
                    UsedAt = now,
                    UsedQty = usedQty
                };

                _db.FoodPromotionUsages.Add(usage);
            }

            _db.SaveChanges();

            HttpContext.Session.Remove("Cart");

            TempData["Success"] = $"ส่งคำสั่งซื้อเรียบร้อยแล้ว (เลขออเดอร์: {newOrderId}) กรุณารอพนักงานยืนยัน";
            return RedirectToAction("OrderStatus");
        }
        // HELPER : ใช้คำนวณส่วนลด (เผื่อใช้ต่อในอนาคต)
        private decimal CalculatePromotionDiscount(FoodPromotion promotion, decimal eligibleAmount)
        {
            if (promotion == null || eligibleAmount <= 0) return 0m;

            var discountType = (promotion.DiscountType ?? "PERCENT").Trim().ToUpper();
            decimal discount = 0m;

            if (discountType == "AMOUNT")
            {
                discount = promotion.DiscountValue;
            }
            else
            {
                discount = eligibleAmount * (promotion.DiscountValue / 100m);
            }

            if (discount < 0) discount = 0m;
            if (discount > eligibleAmount) discount = eligibleAmount;

            return Math.Round(discount, 2);
        }

        // =====================================================
        // HELPER : milestone promotion
        // =====================================================
        private bool CanUseMilestonePromotion(string userId, FoodPromotion promotion)
        {
            if (string.IsNullOrWhiteSpace(userId) || promotion == null) return false;
            if (!promotion.IsMilestonePromotion) return true;
            if (!promotion.RequiredOrderCount.HasValue || promotion.RequiredOrderCount.Value <= 0) return false;

            int requiredCount = promotion.RequiredOrderCount.Value;

            // นับเฉพาะออเดอร์สำเร็จของผู้ใช้
            int successOrderCount = _db.FoodOrders.Count(o =>
                o.UserId == userId &&
                o.OrderStatus == 7UL);

            // ได้สิทธิ์กี่ครั้ง เช่น 23 ออเดอร์ / 10 = 2 สิทธิ์
            int entitledUses = successOrderCount / requiredCount;

            if (entitledUses <= 0) return false;

            // ใช้โปรนี้ไปแล้วกี่ครั้ง
            // milestone ใช้ UsedQty = 1 ต่อ 1 ออเดอร์
            int usedCount = _db.FoodPromotionUsages
                .Where(u => u.UserId == userId && u.PromotionId == promotion.PromotionId)
                .Sum(u => (int?)u.UsedQty) ?? 0;

            return usedCount < entitledUses;
        }

        // EMPLOYEE + OWNER : รับออเดอร์
        // 1 -> 2
        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveOrder(string id)
        {
            var order = _db.FoodOrders.FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์";
                return RedirectToAction("OrderList");
            }

            if (order.OrderStatus != 1UL)
            {
                TempData["Error"] = "ออเดอร์นี้ไม่อยู่ในสถานะรอพนักงานยืนยัน";
                return RedirectToAction("OrderList");
            }

            string? employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            order.OrderStatus = 2UL;
            order.HandledByUserId = employeeId;

            _db.SaveChanges();

            TempData["Success"] = $"รับออเดอร์ {order.OrderId} เรียบร้อยแล้ว";
            return RedirectToAction("OrderList");
        }

        // EMPLOYEE + OWNER : ปฏิเสธออเดอร์
        // 1 -> 8
        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectOrder(string id)
        {
            var order = _db.FoodOrders.FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์";
                return RedirectToAction("OrderList");
            }

            if (order.OrderStatus != 1UL)
            {
                TempData["Error"] = "ออเดอร์นี้ไม่อยู่ในสถานะรอพนักงานยืนยัน";
                return RedirectToAction("OrderList");
            }

            string? employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            order.OrderStatus = 8UL;
            order.HandledByUserId = employeeId;

            _db.SaveChanges();

            TempData["Success"] = $"ปฏิเสธออเดอร์ {order.OrderId} เรียบร้อยแล้ว";
            return RedirectToAction("OrderList");
        }

        // CUSTOMER : ลูกค้ายืนยันการชำระเงิน
        // 2 -> 3
        [Authorize(Roles = "CUSTOMER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPayment(string orderId, PaymentViewModel data)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งาน";
                return RedirectToAction("Login", "Auth");
            }

            var order = _db.FoodOrders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์";
                return RedirectToAction("OrderStatus");
            }

            if (order.OrderStatus != 2UL)
            {
                TempData["Error"] = "ออเดอร์นี้ยังไม่พร้อมสำหรับการชำระเงิน";
                return RedirectToAction("OrderStatus");
            }

            if (string.IsNullOrWhiteSpace(data.PaymentMethod))
            {
                TempData["Error"] = "กรุณาเลือกช่องทางการชำระเงิน";
                return RedirectToAction("OrderStatus");
            }

            string? slipFileName = null;

            if (data.PaymentMethod == "PromptPay" || data.PaymentMethod == "BankTransfer")
            {
                if (data.SlipFile == null || data.SlipFile.Length == 0)
                {
                    TempData["Error"] = "กรุณาอัปโหลดสลิปการชำระเงิน";
                    return RedirectToAction("OrderStatus");
                }

                string slipFolder = Path.Combine(_env.WebRootPath, "img", "slips");

                if (!Directory.Exists(slipFolder))
                {
                    Directory.CreateDirectory(slipFolder);
                }

                string ext = Path.GetExtension(data.SlipFile.FileName);
                slipFileName = Guid.NewGuid().ToString() + ext;
                string savePath = Path.Combine(slipFolder, slipFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    data.SlipFile.CopyTo(stream);
                }
            }

            order.OrderStatus = 3UL;
            order.PaymentMethod = data.PaymentMethod;
            order.PaymentSlip = slipFileName;
            order.OrderNote = data.OrderNote;

            _db.SaveChanges();

            TempData["Success"] = $"ส่งหลักฐานการชำระเงินของออเดอร์ {orderId} เรียบร้อยแล้ว กรุณารอพนักงานตรวจสอบ";
            return RedirectToAction("OrderStatus");
        }

        // EMPLOYEE + OWNER : ยืนยันการชำระเงิน
        // 3 -> 4
        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyPayment(string id)
        {
            var order = _db.FoodOrders.FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์";
                return RedirectToAction("OrderList");
            }

            if (order.OrderStatus != 3UL)
            {
                TempData["Error"] = "ออเดอร์นี้ยังไม่อยู่ในสถานะรอตรวจสอบการชำระเงิน";
                return RedirectToAction("OrderList");
            }

            order.OrderStatus = 4UL;
            _db.SaveChanges();

            TempData["Success"] = $"ยืนยันการชำระเงินของออเดอร์ {id} เรียบร้อยแล้ว";
            return RedirectToAction("OrderList");
        }

        // EMPLOYEE + OWNER : กำลังจัดส่ง
        // 4 -> 5
        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAsDelivering(string id)
        {
            var order = _db.FoodOrders.FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์";
                return RedirectToAction("OrderList");
            }

            if (order.OrderStatus != 4UL)
            {
                TempData["Error"] = "ออเดอร์นี้ยังไม่อยู่ในสถานะพร้อมจัดส่ง";
                return RedirectToAction("OrderList");
            }

            order.OrderStatus = 5UL;
            _db.SaveChanges();

            TempData["Success"] = $"ออเดอร์ {id} อยู่ระหว่างการจัดส่งแล้ว รอลูกค้ายืนยันรับอาหาร";
            return RedirectToAction("OrderList");
        }

        // CUSTOMER : ลูกค้ากดรับอาหารแล้ว
        [Authorize(Roles = "CUSTOMER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmReceived(string id)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งาน";
                return RedirectToAction("Login", "Auth");
            }

            var order = _db.FoodOrders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์";
                return RedirectToAction("OrderStatus");
            }

            var user = _db.FoodUsers.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                TempData["Error"] = "ไม่พบข้อมูลลูกค้า";
                return RedirectToAction("OrderStatus");
            }

            bool isPromotionOrder =
                !string.IsNullOrWhiteSpace(order.PromotionId) ||
                (order.DiscountAmount ?? 0) > 0;

            int calculatedPoints = 0;

            if (!isPromotionOrder && order.FinalPrice > 0)
            {
                // ทุก 100 บาท = 1 แต้ม
                calculatedPoints = (int)Math.Floor(order.FinalPrice / 100m);

                // ถ้ายอดมากกว่า 0 แต่คำนวณแล้วได้ 0 -> ให้ขั้นต่ำ 1 แต้ม
                if (calculatedPoints <= 0)
                {
                    calculatedPoints = 1;
                }
            }

           
            // CASE 1: ออเดอร์สำเร็จแล้ว (status = 7)
            if (order.OrderStatus == 7UL)
            {
                // ถ้าเคยบันทึกแต้มแล้ว หรือเป็นออเดอร์โปรที่ควรได้ 0 แต้มอยู่แล้ว
                if ((order.PointsEarned ?? 0) > 0 || isPromotionOrder)
                {
                    TempData["Error"] = isPromotionOrder
                        ? "ออเดอร์นี้ใช้โปรโมชั่น จึงไม่ได้รับแต้มสะสม"
                        : "ออเดอร์นี้รับอาหารเรียบร้อยแล้ว และได้รับแต้มแล้ว";

                    return RedirectToAction("OrderStatus");
                }

                // ซ่อมข้อมูลย้อนหลังเฉพาะออเดอร์ธรรมดาที่ยังไม่มีแต้ม
                order.PointsEarned = calculatedPoints;

                user.OrderCount = (user.OrderCount ?? 0) + 1;
                user.LoyaltyPoints = (user.LoyaltyPoints ?? 0) + calculatedPoints;

                _db.SaveChanges();

                TempData["Success"] = $"ระบบทำการเพิ่มแต้มย้อนหลังให้ออเดอร์นี้แล้ว 🎉 ได้รับแต้มสะสม +{calculatedPoints} แต้ม";
                return RedirectToAction("OrderStatus");
            }

            
            // CASE 2: ต้องอยู่ในสถานะกำลังจัดส่งเท่านั้น ถึงจะกดรับอาหารได้
            if (order.OrderStatus != 5UL && order.OrderStatus != 6UL)
            {
                TempData["Error"] = "ออเดอร์นี้ยังไม่อยู่ในสถานะกำลังจัดส่ง";
                return RedirectToAction("OrderStatus");
            }

        
            // CASE 3: ปิดออเดอร์ปกติ
            order.OrderStatus = 7UL;
            order.PointsEarned = calculatedPoints;

            user.OrderCount = (user.OrderCount ?? 0) + 1;
            user.LoyaltyPoints = (user.LoyaltyPoints ?? 0) + calculatedPoints;

            _db.SaveChanges();

            TempData["Success"] = isPromotionOrder
                ? "รับอาหารเรียบร้อยแล้ว 🎉 ออเดอร์นี้ใช้โปรโมชั่น จึงไม่ได้รับแต้มสะสม"
                : $"รับอาหารเรียบร้อยแล้ว 🎉 ได้รับแต้มสะสม +{calculatedPoints} แต้ม";

            return RedirectToAction("OrderStatus");
        }

        // SUBMIT REVIEW (CUSTOMER)
        [Authorize(Roles = "CUSTOMER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(string orderId, int score, string? commentReview)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "กรุณาเข้าสู่ระบบก่อน";
                return RedirectToAction("Login", "Auth");
            }

            var order = _db.FoodOrders
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "ไม่พบออเดอร์ที่ต้องการรีวิว";
                return RedirectToAction("OrderStatus");
            }

            if (order.OrderStatus != 7UL)
            {
                TempData["Error"] = "ออเดอร์นี้ยังไม่สามารถรีวิวได้";
                return RedirectToAction("OrderStatus");
            }

            if (score < 1 || score > 5)
            {
                TempData["Error"] = "กรุณาเลือกคะแนน 1 - 5";
                return RedirectToAction("OrderStatus");
            }

            var alreadyReview = _db.FoodReviews.Any(r => r.OrderId == orderId);
            if (alreadyReview)
            {
                TempData["Error"] = "ออเดอร์นี้ถูกรีวิวไปแล้ว";
                return RedirectToAction("OrderStatus");
            }

            var lastReview = _db.FoodReviews
                .AsEnumerable()
                .Where(r => !string.IsNullOrEmpty(r.ReviewId) && r.ReviewId.StartsWith("RV") && r.ReviewId.Length >= 4)
                .OrderByDescending(r => r.ReviewId)
                .FirstOrDefault();

            int next = 1;
            if (lastReview != null && int.TryParse(lastReview.ReviewId.Substring(2), out int lastNum))
            {
                next = lastNum + 1;
            }

            string newReviewId = "RV" + next.ToString("D3");

            var review = new FoodReview
            {
                ReviewId = newReviewId,
                OrderId = order.OrderId,
                UserId = userId,
                Score = (byte)score,
                CommentReview = string.IsNullOrWhiteSpace(commentReview) ? null : commentReview.Trim(),
                ReviewDate = DateTime.Now
            };

            _db.FoodReviews.Add(review);

            order.ReviewDecision = (byte)1;
            order.ReviewDecidedAt = DateTime.Now;

            _db.SaveChanges();

            TempData["Success"] = "ส่งรีวิวเรียบร้อยแล้ว";
            return RedirectToAction("OrderStatus");
        }

        // CUSTOMER : ใช้สำหรับ Auto Refresh หน้า OrderStatus
        [Authorize(Roles = "CUSTOMER")]
        [HttpGet]
        public IActionResult GetOrderStatusData()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var orders = _db.FoodOrders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.OrderTime,
                    o.FinalPrice,
                    o.OrderStatus,
                    o.PaymentMethod,
                    o.PaymentSlip,
                    o.OrderNote,
                    o.PointsEarned,
                    o.PromotionId,
                    o.ReviewDecision,
                    o.ShippingReceiverName,
                    o.ShippingPhone,
                    o.ShippingAddressDetail,
                    o.ShippingSubDistrict,
                    o.ShippingDistrict,
                    o.ShippingProvince
                })
                .ToList();

            return Json(orders);
        }

        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpGet]
        public IActionResult GetOrderListData()
        {
            var orders = _db.FoodOrders
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderTime)
                .Select(o => new
                {
                    o.OrderId,
                    o.UserId,
                    o.OrderDate,
                    o.OrderTime,
                    o.TotalPrice,
                    o.DiscountAmount,
                    o.FinalPrice,
                    o.OrderStatus,
                    o.PaymentMethod,
                    o.PaymentSlip,
                    o.OrderNote,
                    o.PointsEarned,
                    o.PromotionId,
                    o.ReviewDecision,
                    o.ShippingReceiverName,
                    o.ShippingPhone,
                    o.ShippingAddressDetail,
                    o.ShippingSubDistrict,
                    o.ShippingDistrict,
                    o.ShippingProvince
                })
                .ToList();

            return Json(orders);
        }

        // Helper : สร้าง OrderId
        private string GenerateNextOrderId()
        {
            var lastOrder = _db.FoodOrders
                .AsEnumerable()
                .Where(o => !string.IsNullOrEmpty(o.OrderId) && o.OrderId.StartsWith("O"))
                .OrderByDescending(o => o.OrderId)
                .FirstOrDefault();

            int next = 1;

            if (lastOrder != null)
            {
                string numberPart = new string(lastOrder.OrderId.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrWhiteSpace(numberPart) && int.TryParse(numberPart, out int lastNum))
                {
                    next = lastNum + 1;
                }
            }

            return "O" + next.ToString("D3");
        }

        // Helper : สร้าง OrderItemId
        private int GetNextOrderItemNumber()
        {
            var lastOrderItem = _db.FoodOrderitems
                .AsEnumerable()
                .Where(x => !string.IsNullOrEmpty(x.OrderItemId) && x.OrderItemId.StartsWith("OI"))
                .OrderByDescending(x => x.OrderItemId)
                .FirstOrDefault();

            int next = 1;

            if (lastOrderItem != null)
            {
                string numberPart = new string(lastOrderItem.OrderItemId.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrWhiteSpace(numberPart) && int.TryParse(numberPart, out int lastNum))
                {
                    next = lastNum + 1;
                }
            }

            return next;
        }

        // Helper : สร้าง PromotionUsageId
        private string GenerateNextPromotionUsageId()
        {
            var lastUsage = _db.FoodPromotionUsages
                .AsEnumerable()
                .Where(x => !string.IsNullOrEmpty(x.UsageId) && x.UsageId.StartsWith("PU"))
                .OrderByDescending(x => x.UsageId)
                .FirstOrDefault();

            int next = 1;

            if (lastUsage != null)
            {
                string numberPart = new string(lastUsage.UsageId.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrWhiteSpace(numberPart) && int.TryParse(numberPart, out int lastNum))
                {
                    next = lastNum + 1;
                }
            }

            return "PU" + next.ToString("D3");
        }

        // Helper : สร้าง AddressId
        private string GenerateNextAddressId()
        {
            var allAddressIds = _db.FoodAddresses
                .AsEnumerable()
                .Select(a => a.AddressId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            int maxNumber = 0;

            foreach (var id in allAddressIds)
            {
                string numberPart = new string(id.Where(char.IsDigit).ToArray());

                if (!string.IsNullOrWhiteSpace(numberPart) && int.TryParse(numberPart, out int num))
                {
                    if (num > maxNumber)
                        maxNumber = num;
                }
            }

            int next = maxNumber + 1;

            return "ADDR" + next.ToString("D3");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}