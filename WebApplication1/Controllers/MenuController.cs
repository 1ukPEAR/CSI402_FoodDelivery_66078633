using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.FoodViewModels;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers
{
    public class MenuController : Controller
    {
        private readonly FooddeliverydbContext _db;

        public MenuController(FooddeliverydbContext db)
        {
            _db = db;
        }

        // =========================
        // OWNER ONLY : Manage Menu
        // =========================
        [Authorize(Roles = "OWNER")]
        [HttpGet]
        public IActionResult ManageMenu()
        {
            var menus = _db.FoodMenus
                .Include(m => m.MenuType)
                .Include(m => m.FoodMenuoptions)
                .OrderBy(m => m.MenuId)
                .ToList();

            ViewBag.MenuTypes = _db.FoodMenutypes
                .OrderBy(t => t.MenuTypeId)
                .ToList();

            return View(menus);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManageMenu(MenuViewModel data, IFormFile? MenuImageFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.MenuName) || string.IsNullOrWhiteSpace(data.MenuTypeId))
                {
                    TempData["Error"] = "Please fill in required fields.";
                    return RedirectToAction("ManageMenu");
                }

                var lastMenu = _db.FoodMenus
                    .AsEnumerable()
                    .Where(m => !string.IsNullOrEmpty(m.MenuId) && m.MenuId.StartsWith("M") && m.MenuId.Length >= 4)
                    .OrderByDescending(m => m.MenuId)
                    .FirstOrDefault();

                int next = 1;
                if (lastMenu != null && int.TryParse(lastMenu.MenuId.Substring(1), out int lastNum))
                {
                    next = lastNum + 1;
                }

                string newMenuId = "M" + next.ToString("D3");

                string imagePath = "/img/menu/default-food.jpg";

                if (MenuImageFile != null && MenuImageFile.Length > 0)
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    string extension = Path.GetExtension(MenuImageFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = "Only JPG, JPEG, PNG, GIF, WEBP files are allowed.";
                        return RedirectToAction("ManageMenu");
                    }

                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "menu");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string fileName = newMenuId + extension;
                    string fullPath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        MenuImageFile.CopyTo(stream);
                    }

                    imagePath = "/img/menu/" + fileName;
                }

                var m = new FoodMenu
                {
                    MenuId = newMenuId,
                    MenuName = data.MenuName,
                    MenuPrice = data.MenuPrice,
                    MenuTypeId = data.MenuTypeId,
                    MenuStatus = 1UL,
                    MenuImage = imagePath
                };

                _db.FoodMenus.Add(m);
                _db.SaveChanges();

                int nextOpt = 1;
                var lastOpt = _db.FoodMenuoptions
                    .AsEnumerable()
                    .Where(o => !string.IsNullOrEmpty(o.OptionId) && o.OptionId.StartsWith("O") && o.OptionId.Length >= 4)
                    .OrderByDescending(o => o.OptionId)
                    .FirstOrDefault();

                if (lastOpt != null && int.TryParse(lastOpt.OptionId.Substring(1), out int lastOptNum))
                {
                    nextOpt = lastOptNum + 1;
                }

                if (data.OptionName != null)
                {
                    for (int i = 0; i < data.OptionName.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(data.OptionName[i]))
                        {
                            decimal extraPrice = 0;

                            if (data.ExtraPrice != null && i < data.ExtraPrice.Count)
                            {
                                extraPrice = data.ExtraPrice[i] ?? 0;
                            }

                            var o = new FoodMenuoption
                            {
                                OptionId = "O" + nextOpt.ToString("D3"),
                                MenuId = newMenuId,
                                OptionName = data.OptionName[i]!,
                                ExtraPrice = extraPrice
                            };

                            _db.FoodMenuoptions.Add(o);
                            nextOpt++;
                        }
                    }

                    _db.SaveChanges();
                }

                TempData["Success"] = "Menu added successfully!";
                return RedirectToAction("ManageMenu");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("ManageMenu");
            }
        }

        // =========================
        // OWNER : Edit Menu (GET)
        // =========================
        [Authorize(Roles = "OWNER")]
        [HttpGet]
        public IActionResult EditMenu(string id)
        {
            var menu = _db.FoodMenus
                .Include(m => m.FoodMenuoptions)
                .FirstOrDefault(m => m.MenuId == id);

            if (menu == null)
            {
                TempData["Error"] = "ไม่พบเมนูที่ต้องการแก้ไข";
                return RedirectToAction("ManageMenu");
            }

            ViewBag.MenuTypes = _db.FoodMenutypes
                .OrderBy(t => t.MenuTypeId)
                .ToList();

            return View(menu);
        }

        // =========================
        // OWNER : Edit Menu (POST)
        // =========================
        [Authorize(Roles = "OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditMenu(FoodMenu data, IFormFile? MenuImageFile, List<string>? OptionName, List<decimal?>? ExtraPrice)
        {
            try
            {
                var menu = _db.FoodMenus
                    .Include(m => m.FoodMenuoptions)
                    .FirstOrDefault(m => m.MenuId == data.MenuId);

                if (menu == null)
                {
                    TempData["Error"] = "ไม่พบเมนูที่ต้องการแก้ไข";
                    return RedirectToAction("ManageMenu");
                }

                menu.MenuName = data.MenuName;
                menu.MenuPrice = data.MenuPrice;
                menu.MenuTypeId = data.MenuTypeId;

                // อัปโหลดรูปใหม่
                if (MenuImageFile != null && MenuImageFile.Length > 0)
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    string extension = Path.GetExtension(MenuImageFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = "Only JPG, JPEG, PNG, GIF, WEBP files are allowed.";
                        return RedirectToAction("ManageMenu");
                    }

                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "menu");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string fileName = menu.MenuId + extension;
                    string fullPath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        MenuImageFile.CopyTo(stream);
                    }

                    menu.MenuImage = "/img/menu/" + fileName;
                }

                // ลบ option เดิมทั้งหมด
                var oldOptions = _db.FoodMenuoptions.Where(o => o.MenuId == menu.MenuId).ToList();
                if (oldOptions.Any())
                {
                    _db.FoodMenuoptions.RemoveRange(oldOptions);
                }

                // หา next option id
                int nextOpt = 1;
                var lastOpt = _db.FoodMenuoptions
                    .AsEnumerable()
                    .Where(o => !string.IsNullOrEmpty(o.OptionId) && o.OptionId.StartsWith("O") && o.OptionId.Length >= 4)
                    .OrderByDescending(o => o.OptionId)
                    .FirstOrDefault();

                if (lastOpt != null && int.TryParse(lastOpt.OptionId.Substring(1), out int lastOptNum))
                {
                    nextOpt = lastOptNum + 1;
                }

                // เพิ่ม option ใหม่
                if (OptionName != null)
                {
                    for (int i = 0; i < OptionName.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(OptionName[i]))
                        {
                            decimal extra = 0;
                            if (ExtraPrice != null && i < ExtraPrice.Count)
                            {
                                extra = ExtraPrice[i] ?? 0;
                            }

                            _db.FoodMenuoptions.Add(new FoodMenuoption
                            {
                                OptionId = "O" + nextOpt.ToString("D3"),
                                MenuId = menu.MenuId,
                                OptionName = OptionName[i],
                                ExtraPrice = extra
                            });

                            nextOpt++;
                        }
                    }
                }

                _db.SaveChanges();

                TempData["Success"] = "แก้ไขเมนูเรียบร้อยแล้ว";
                return RedirectToAction("ManageMenu");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "เกิดข้อผิดพลาด: " + ex.Message;
                return RedirectToAction("ManageMenu");
            }
        }

        // =========================
        // OWNER : Delete Menu
        // =========================
        [Authorize(Roles = "OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMenu(string id)
        {
            try
            {
                var menu = _db.FoodMenus
                    .Include(m => m.FoodMenuoptions)
                    .FirstOrDefault(m => m.MenuId == id);

                if (menu == null)
                {
                    TempData["Error"] = "ไม่พบเมนูที่ต้องการลบ";
                    return RedirectToAction("ManageMenu");
                }

                // ลบ options ก่อน
                if (menu.FoodMenuoptions != null && menu.FoodMenuoptions.Any())
                {
                    _db.FoodMenuoptions.RemoveRange(menu.FoodMenuoptions);
                }

                // ลบความสัมพันธ์โปรโมชั่นของเมนูนี้ก่อน (สำคัญ)
                var promoLinks = _db.FoodPromotionMenus.Where(pm => pm.MenuId == menu.MenuId).ToList();
                if (promoLinks.Any())
                {
                    _db.FoodPromotionMenus.RemoveRange(promoLinks);
                }

                // ลบรูป (ถ้าไม่ใช่ default)
                if (!string.IsNullOrWhiteSpace(menu.MenuImage) &&
                    menu.MenuImage != "/img/menu/default-food.jpg")
                {
                    var imagePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        menu.MenuImage.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                    );

                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _db.FoodMenus.Remove(menu);
                _db.SaveChanges();

                TempData["Success"] = $"ลบเมนู '{menu.MenuName}' เรียบร้อยแล้ว";
                return RedirectToAction("ManageMenu");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "เกิดข้อผิดพลาดในการลบเมนู: " + ex.Message;
                return RedirectToAction("ManageMenu");
            }
        }

        // =========================
        // PUBLIC PAGE : User Menu + Active Promotions
        // =========================
        [AllowAnonymous]
        [HttpGet]
        public IActionResult MenuUser()
        {
            var now = DateTime.Now;

            // =========================================================
            // ดึงเมนูที่เปิดขายจริง + Include ให้ view ใช้งานครบ
            // =========================================================
            var menus = _db.FoodMenus
                .Include(m => m.MenuType)
                .Include(m => m.FoodMenuoptions)
                .Where(m => m.MenuStatus == 1UL)
                .OrderBy(m => m.MenuId)
                .ToList();

            // =========================================================
            // ดึงโปรโมชั่นที่ active จริง
            // =========================================================
            var promotions = _db.FoodPromotions
                .Where(p =>
                    (!p.StartDate.HasValue || p.StartDate.Value <= now) &&
                    (!p.EndDate.HasValue || p.EndDate.Value >= now)
                )
                .OrderByDescending(p =>
                    p.DiscountValue > 0
                        ? p.DiscountValue
                        : (decimal)(p.DiscountPercent ?? 0))
                .ThenBy(p => p.StartDate)
                .ToList();

            // =========================================================
            // NEW: ซ่อน milestone promotion ถ้ายังไม่ถึง / ใช้สิทธิ์หมดแล้ว
            // =========================================================
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var filteredPromotions = new List<FoodPromotion>();

                foreach (var promo in promotions)
                {
                    // โปรปกติ แสดงได้เลย
                    if (!promo.IsMilestonePromotion)
                    {
                        filteredPromotions.Add(promo);
                        continue;
                    }

                    // ถ้าเป็น milestone แต่ตั้งค่าไม่ครบ => ไม่แสดง
                    if (!promo.RequiredOrderCount.HasValue || promo.RequiredOrderCount.Value <= 0)
                    {
                        continue;
                    }

                    int requiredCount = promo.RequiredOrderCount.Value;

                    // นับเฉพาะออเดอร์สำเร็จของ user
                    int successOrderCount = _db.FoodOrders.Count(o =>
                        o.UserId == userId &&
                        o.OrderStatus == 7UL);

                    // สิทธิ์ที่ควรมี เช่น 23 / 10 = 2 สิทธิ์
                    int entitledUses = successOrderCount / requiredCount;

                    // ถ้ายังไม่มีสิทธิ์ ไม่ต้องแสดง
                    if (entitledUses <= 0)
                    {
                        continue;
                    }

                    // ใช้ไปแล้วกี่สิทธิ์
                    int usedCount = _db.FoodPromotionUsages
                        .Where(u => u.UserId == userId && u.PromotionId == promo.PromotionId)
                        .Sum(u => (int?)u.UsedQty) ?? 0;

                    // แสดงเฉพาะตอนยังมีสิทธิ์เหลือ
                    if (usedCount < entitledUses)
                    {
                        filteredPromotions.Add(promo);
                    }
                }

                promotions = filteredPromotions;
            }
            else
            {
                // ยังไม่ login => ซ่อน milestone promotion ทั้งหมด
                promotions = promotions
                    .Where(p => !p.IsMilestonePromotion)
                    .ToList();
            }

            // =========================================================
            // ดึง mapping โปร <-> เมนู
            // =========================================================
            var promotionMenus = _db.FoodPromotionMenus.ToList();

            // =========================================================
            // เมนูตัวโชว์ของแต่ละโปร
            // - หา "เมนูตัวแรกที่มีอยู่จริงใน menus"
            // - กันกรณีเมนูตัวแรกใน mapping ถูกปิด / ไม่อยู่ในหน้า
            // =========================================================
            var promoDisplayMenus = new Dictionary<string, FoodMenu>();

            foreach (var promo in promotions)
            {
                var promoMenuIds = promotionMenus
                    .Where(pm => pm.PromotionId == promo.PromotionId)
                    .Select(pm => pm.MenuId)
                    .Distinct()
                    .ToList();

                var firstValidMenu = menus.FirstOrDefault(m => promoMenuIds.Contains(m.MenuId));

                if (firstValidMenu != null)
                {
                    promoDisplayMenus[promo.PromotionId] = firstValidMenu;
                }
            }

            // รองรับทั้ง View เวอร์ชันเก่าและใหม่
            ViewBag.Promotions = promotions;
            ViewBag.ActivePromotions = promotions;
            ViewBag.PromotionMenus = promotionMenus;
            ViewBag.PromoDisplayMenus = promoDisplayMenus;
            ViewBag.MenuTypes = _db.FoodMenutypes.OrderBy(t => t.MenuTypeId).ToList();

            return View(menus);
        }

        // =========================
        // EMPLOYEE / OWNER : หน้าเปิด-ปิดเมนู
        // =========================
        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpGet]
        public IActionResult MenuStatus()
        {
            var menus = _db.FoodMenus
                .Include(m => m.MenuType)
                .Include(m => m.FoodMenuoptions)
                .OrderBy(m => m.MenuId)
                .ToList();

            return View(menus);
        }

        // =========================
        // EMPLOYEE / OWNER : สลับสถานะเมนู
        // =========================
        [Authorize(Roles = "EMPLOYEE,OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleMenuStatus(string id)
        {
            var menu = _db.FoodMenus.FirstOrDefault(m => m.MenuId == id);

            if (menu == null)
            {
                TempData["Error"] = "ไม่พบเมนูที่ต้องการ";
                return RedirectToAction("MenuStatus");
            }

            // 1 = พร้อมขาย / 0 = ปิดเมนู
            menu.MenuStatus = (menu.MenuStatus == 1UL) ? 0UL : 1UL;

            _db.SaveChanges();

            TempData["Success"] = $"อัปเดตสถานะเมนู '{menu.MenuName}' เรียบร้อยแล้ว";
            return RedirectToAction("MenuStatus");
        }

        // =========================
        // OWNER : Update Menu (สำรองอีก action ตามของเดิม)
        // =========================
        [Authorize(Roles = "OWNER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMenu(FoodMenu data, IFormFile? MenuImageFile, List<string>? OptionName, List<decimal?>? ExtraPrice)
        {
            try
            {
                var menu = _db.FoodMenus
                    .Include(m => m.FoodMenuoptions)
                    .FirstOrDefault(m => m.MenuId == data.MenuId);

                if (menu == null)
                {
                    TempData["Error"] = "ไม่พบเมนูที่ต้องการแก้ไข";
                    return RedirectToAction("ManageMenu");
                }

                menu.MenuName = data.MenuName;
                menu.MenuPrice = data.MenuPrice;
                menu.MenuTypeId = data.MenuTypeId;

                if (MenuImageFile != null && MenuImageFile.Length > 0)
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    string extension = Path.GetExtension(MenuImageFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = "Only JPG, JPEG, PNG, GIF, WEBP files are allowed.";
                        return RedirectToAction("ManageMenu");
                    }

                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "menu");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string fileName = menu.MenuId + extension;
                    string fullPath = Path.Combine(uploadFolder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        MenuImageFile.CopyTo(stream);
                    }

                    menu.MenuImage = "/img/menu/" + fileName;
                }

                var oldOptions = _db.FoodMenuoptions.Where(o => o.MenuId == menu.MenuId).ToList();
                if (oldOptions.Any())
                {
                    _db.FoodMenuoptions.RemoveRange(oldOptions);
                }

                int nextOpt = 1;
                var lastOpt = _db.FoodMenuoptions
                    .AsEnumerable()
                    .Where(o => !string.IsNullOrEmpty(o.OptionId) && o.OptionId.StartsWith("O") && o.OptionId.Length >= 4)
                    .OrderByDescending(o => o.OptionId)
                    .FirstOrDefault();

                if (lastOpt != null && int.TryParse(lastOpt.OptionId.Substring(1), out int lastOptNum))
                {
                    nextOpt = lastOptNum + 1;
                }

                if (OptionName != null)
                {
                    for (int i = 0; i < OptionName.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(OptionName[i]))
                        {
                            decimal extra = 0;
                            if (ExtraPrice != null && i < ExtraPrice.Count)
                            {
                                extra = ExtraPrice[i] ?? 0;
                            }

                            _db.FoodMenuoptions.Add(new FoodMenuoption
                            {
                                OptionId = "O" + nextOpt.ToString("D3"),
                                MenuId = menu.MenuId,
                                OptionName = OptionName[i],
                                ExtraPrice = extra
                            });

                            nextOpt++;
                        }
                    }
                }

                _db.SaveChanges();

                TempData["Success"] = "แก้ไขเมนูเรียบร้อยแล้ว";
                return RedirectToAction("ManageMenu");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "เกิดข้อผิดพลาด: " + ex.Message;
                return RedirectToAction("ManageMenu");
            }
        }
    }
}