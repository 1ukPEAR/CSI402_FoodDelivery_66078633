using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.FoodViewModels;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers
{
    public class PromotionController : Controller
    {
        private readonly FooddeliverydbContext _db;

        public PromotionController(FooddeliverydbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult ManagePromotion()
        {
            var promotions = _db.FoodPromotions
                .OrderBy(p => p.PromotionId)
                .ToList();

            var allMenus = _db.FoodMenus
                .Include(m => m.MenuType)
                .OrderBy(m => m.MenuId)
                .ToList();

            var promotionMenus = _db.FoodPromotionMenus
                .ToList();

            ViewBag.Menus = allMenus;
            ViewBag.AllMenus = allMenus;
            ViewBag.PromotionMenus = promotionMenus;

            return View(promotions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ManagePromotion(PromotionViewModel data)
        {
            // -------------------------
            // normalize ปี พ.ศ./ค.ศ.
            // -------------------------
            data.StartDatePromotion = NormalizeThaiYear(data.StartDatePromotion);
            data.EndDatePromotion = NormalizeThaiYear(data.EndDatePromotion);

            // -------------------------
            // normalize discount type
            // -------------------------
            data.DiscountType = string.IsNullOrWhiteSpace(data.DiscountType)
                ? "PERCENT"
                : data.DiscountType.Trim().ToUpper();

            // -------------------------
            // validation
            // -------------------------
            if (string.IsNullOrWhiteSpace(data.PromotionName))
            {
                ModelState.AddModelError("", "กรุณาระบุชื่อโปรโมชั่น");
            }

            if (string.IsNullOrWhiteSpace(data.DiscountType))
            {
                ModelState.AddModelError("", "กรุณาเลือกประเภทส่วนลด");
            }

            if (data.DiscountType == "PERCENT")
            {
                if (data.DiscountValue <= 0 || data.DiscountValue > 100)
                {
                    ModelState.AddModelError("", "ส่วนลดแบบเปอร์เซ็นต์ต้องอยู่ระหว่าง 1 - 100%");
                }
            }
            else if (data.DiscountType == "AMOUNT")
            {
                if (data.DiscountValue <= 0)
                {
                    ModelState.AddModelError("", "ส่วนลดแบบจำนวนเงินต้องมากกว่า 0 บาท");
                }
            }
            else
            {
                ModelState.AddModelError("", "ประเภทส่วนลดไม่ถูกต้อง");
            }

            if (data.StartDatePromotion > data.EndDatePromotion)
            {
                ModelState.AddModelError("", "วันเวลาเริ่มโปรโมชั่นต้องไม่มากกว่าวันเวลาสิ้นสุด");
            }

            if (data.SelectedMenuIds == null || !data.SelectedMenuIds.Any())
            {
                ModelState.AddModelError("", "กรุณาเลือกอย่างน้อย 1 เมนูสำหรับโปรโมชั่น");
            }

            if (data.MaxUsePerUser.HasValue && data.MaxUsePerUser.Value <= 0)
            {
                ModelState.AddModelError("", "จำนวนครั้งต่อผู้ใช้ต้องมากกว่า 0");
            }

            if (data.MaxUsePerOrder.HasValue && data.MaxUsePerOrder.Value <= 0)
            {
                ModelState.AddModelError("", "จำนวนครั้งต่อออเดอร์ต้องมากกว่า 0");
            }

            // =========================
            // NEW: validation โปรซื้อครบทุก X ครั้ง
            // =========================
            if (data.IsMilestonePromotion)
            {
                if (!data.RequiredOrderCount.HasValue || data.RequiredOrderCount.Value <= 0)
                {
                    ModelState.AddModelError("", "กรุณาระบุจำนวนครั้งที่ต้องซื้อให้ถูกต้อง");
                }

                // แนะนำให้ milestone ใช้ครั้งละ 1 สิทธิ์ต่อออเดอร์
                if (data.MaxUsePerOrder.HasValue && data.MaxUsePerOrder.Value > 1)
                {
                    ModelState.AddModelError("", "โปรโมชั่นซื้อครบทุก X ครั้ง แนะนำให้จำกัดต่อออเดอร์ไม่เกิน 1");
                }
            }

            if (!ModelState.IsValid)
            {
                LoadPromotionPageData();
                TempData["ErrorMessage"] = "ข้อมูลโปรโมชั่นไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง";

                var promotions = _db.FoodPromotions
                    .OrderBy(p => p.PromotionId)
                    .ToList();

                return View("ManagePromotion", promotions);
            }

            try
            {
                var p = new FoodPromotion
                {
                    PromotionId = Guid.NewGuid().ToString("N").Substring(0, 10),
                    PromotionName = data.PromotionName.Trim(),
                    DescriptionPro = string.IsNullOrWhiteSpace(data.DescriptionPro) ? null : data.DescriptionPro.Trim(),

                    // เก็บของเดิมไว้เพื่อ compatibility
                    DiscountPercent = data.DiscountType == "PERCENT"
                        ? (sbyte?)Convert.ToSByte(Math.Min(data.DiscountValue, 100))
                        : (sbyte?)0,

                    // ของใหม่
                    DiscountType = data.DiscountType,
                    DiscountValue = data.DiscountValue,

                    // ใช้ DateTime จริง
                    StartDate = data.StartDatePromotion,
                    EndDate = data.EndDatePromotion,

                    // limit
                    MaxUsePerUser = data.MaxUsePerUser,
                    MaxUsePerOrder = data.MaxUsePerOrder,

                    // =========================
                    // NEW: milestone promotion
                    // =========================
                    IsMilestonePromotion = data.IsMilestonePromotion,
                    RequiredOrderCount = data.IsMilestonePromotion ? data.RequiredOrderCount : null
                };

                _db.FoodPromotions.Add(p);
                _db.SaveChanges();

                foreach (var menuId in data.SelectedMenuIds.Distinct())
                {
                    _db.FoodPromotionMenus.Add(new FoodPromotionMenu
                    {
                        PromotionId = p.PromotionId,
                        MenuId = menuId
                    });
                }

                _db.SaveChanges();

                TempData["SuccessMessage"] = "สร้างโปรโมชั่นสำเร็จแล้ว";
                return RedirectToAction("ManagePromotion");
            }
            catch (Exception ex)
            {
                LoadPromotionPageData();
                TempData["ErrorMessage"] = "บันทึกโปรโมชั่นไม่สำเร็จ: " + ex.Message;

                var promotions = _db.FoodPromotions
                    .OrderBy(p => p.PromotionId)
                    .ToList();

                return View("ManagePromotion", promotions);
            }
        }

        // =========================================================
        // EDIT PROMOTION
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPromotion(PromotionViewModel data)
        {
            // -------------------------
            // normalize ปี พ.ศ./ค.ศ.
            // -------------------------
            data.StartDatePromotion = NormalizeThaiYear(data.StartDatePromotion);
            data.EndDatePromotion = NormalizeThaiYear(data.EndDatePromotion);

            // -------------------------
            // normalize discount type
            // -------------------------
            data.DiscountType = string.IsNullOrWhiteSpace(data.DiscountType)
                ? "PERCENT"
                : data.DiscountType.Trim().ToUpper();

            // -------------------------
            // validation
            // -------------------------
            if (string.IsNullOrWhiteSpace(data.PromotionId))
            {
                TempData["ErrorMessage"] = "ไม่พบรหัสโปรโมชั่น";
                return RedirectToAction("ManagePromotion");
            }

            if (string.IsNullOrWhiteSpace(data.PromotionName))
            {
                TempData["ErrorMessage"] = "กรุณาระบุชื่อโปรโมชั่น";
                return RedirectToAction("ManagePromotion");
            }

            if (data.DiscountType == "PERCENT")
            {
                if (data.DiscountValue <= 0 || data.DiscountValue > 100)
                {
                    TempData["ErrorMessage"] = "ส่วนลดแบบเปอร์เซ็นต์ต้องอยู่ระหว่าง 1 - 100%";
                    return RedirectToAction("ManagePromotion");
                }
            }
            else if (data.DiscountType == "AMOUNT")
            {
                if (data.DiscountValue <= 0)
                {
                    TempData["ErrorMessage"] = "ส่วนลดแบบจำนวนเงินต้องมากกว่า 0 บาท";
                    return RedirectToAction("ManagePromotion");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "ประเภทส่วนลดไม่ถูกต้อง";
                return RedirectToAction("ManagePromotion");
            }

            if (data.StartDatePromotion > data.EndDatePromotion)
            {
                TempData["ErrorMessage"] = "วันเวลาเริ่มโปรโมชั่นต้องไม่มากกว่าวันเวลาสิ้นสุด";
                return RedirectToAction("ManagePromotion");
            }

            if (data.SelectedMenuIds == null || !data.SelectedMenuIds.Any())
            {
                TempData["ErrorMessage"] = "กรุณาเลือกอย่างน้อย 1 เมนูสำหรับโปรโมชั่น";
                return RedirectToAction("ManagePromotion");
            }

            if (data.MaxUsePerUser.HasValue && data.MaxUsePerUser.Value <= 0)
            {
                TempData["ErrorMessage"] = "จำนวนครั้งต่อผู้ใช้ต้องมากกว่า 0";
                return RedirectToAction("ManagePromotion");
            }

            if (data.MaxUsePerOrder.HasValue && data.MaxUsePerOrder.Value <= 0)
            {
                TempData["ErrorMessage"] = "จำนวนครั้งต่อออเดอร์ต้องมากกว่า 0";
                return RedirectToAction("ManagePromotion");
            }

            // =========================
            // NEW: validation โปรซื้อครบทุก X ครั้ง
            // =========================
            if (data.IsMilestonePromotion)
            {
                if (!data.RequiredOrderCount.HasValue || data.RequiredOrderCount.Value <= 0)
                {
                    TempData["ErrorMessage"] = "กรุณาระบุจำนวนครั้งที่ต้องซื้อให้ถูกต้อง";
                    return RedirectToAction("ManagePromotion");
                }

                if (data.MaxUsePerOrder.HasValue && data.MaxUsePerOrder.Value > 1)
                {
                    TempData["ErrorMessage"] = "โปรโมชั่นซื้อครบทุก X ครั้ง แนะนำให้จำกัดต่อออเดอร์ไม่เกิน 1";
                    return RedirectToAction("ManagePromotion");
                }
            }

            try
            {
                var promotion = _db.FoodPromotions
                    .FirstOrDefault(p => p.PromotionId == data.PromotionId);

                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบโปรโมชั่นที่ต้องการแก้ไข";
                    return RedirectToAction("ManagePromotion");
                }

                // update ตัวโปร
                promotion.PromotionName = data.PromotionName.Trim();
                promotion.DescriptionPro = string.IsNullOrWhiteSpace(data.DescriptionPro) ? null : data.DescriptionPro.Trim();

                promotion.DiscountType = data.DiscountType;
                promotion.DiscountValue = data.DiscountValue;

                promotion.DiscountPercent = data.DiscountType == "PERCENT"
                    ? (sbyte?)Convert.ToSByte(Math.Min(data.DiscountValue, 100))
                    : (sbyte?)0;

                promotion.StartDate = data.StartDatePromotion;
                promotion.EndDate = data.EndDatePromotion;

                promotion.MaxUsePerUser = data.MaxUsePerUser;
                promotion.MaxUsePerOrder = data.MaxUsePerOrder;

                // =========================
                // NEW: milestone promotion
                // =========================
                promotion.IsMilestonePromotion = data.IsMilestonePromotion;
                promotion.RequiredOrderCount = data.IsMilestonePromotion ? data.RequiredOrderCount : null;

                _db.FoodPromotions.Update(promotion);
                _db.SaveChanges();

                // ลบเมนูเก่าทิ้งก่อน
                var oldMenus = _db.FoodPromotionMenus
                    .Where(x => x.PromotionId == data.PromotionId)
                    .ToList();

                if (oldMenus.Any())
                {
                    _db.FoodPromotionMenus.RemoveRange(oldMenus);
                    _db.SaveChanges();
                }

                // เพิ่มเมนูใหม่
                foreach (var menuId in data.SelectedMenuIds.Distinct())
                {
                    _db.FoodPromotionMenus.Add(new FoodPromotionMenu
                    {
                        PromotionId = data.PromotionId,
                        MenuId = menuId
                    });
                }

                _db.SaveChanges();

                TempData["SuccessMessage"] = "แก้ไขโปรโมชั่นสำเร็จแล้ว";
                return RedirectToAction("ManagePromotion");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "แก้ไขโปรโมชั่นไม่สำเร็จ: " + ex.Message;
                return RedirectToAction("ManagePromotion");
            }
        }

        // =========================================================
        // DELETE PROMOTION
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePromotion(string promotionId)
        {
            if (string.IsNullOrWhiteSpace(promotionId))
            {
                TempData["ErrorMessage"] = "ไม่พบรหัสโปรโมชั่น";
                return RedirectToAction("ManagePromotion");
            }

            try
            {
                var promotion = _db.FoodPromotions
                    .FirstOrDefault(p => p.PromotionId == promotionId);

                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "ไม่พบโปรโมชั่นที่ต้องการลบ";
                    return RedirectToAction("ManagePromotion");
                }

                // ถ้าโปรเคยถูกใช้ในออเดอร์แล้ว ไม่ให้ลบ
                bool usedInOrder = _db.FoodOrders.Any(o => o.PromotionId == promotionId);
                if (usedInOrder)
                {
                    TempData["ErrorMessage"] = "โปรโมชั่นนี้ถูกใช้งานแล้ว ไม่สามารถลบได้";
                    return RedirectToAction("ManagePromotion");
                }

                // ถ้ามีตาราง usage และเคยถูกใช้จริง ก็ไม่ให้ลบ
                try
                {
                    bool usedInUsage = _db.FoodPromotionUsages.Any(u => u.PromotionId == promotionId);
                    if (usedInUsage)
                    {
                        TempData["ErrorMessage"] = "โปรโมชั่นนี้มีประวัติการใช้งานแล้ว ไม่สามารถลบได้";
                        return RedirectToAction("ManagePromotion");
                    }
                }
                catch
                {
                    // ถ้าไม่มีตารางนี้หรือ query ไม่ได้ ข้ามไป
                }

                var promotionMenus = _db.FoodPromotionMenus
                    .Where(pm => pm.PromotionId == promotionId)
                    .ToList();

                if (promotionMenus.Any())
                {
                    _db.FoodPromotionMenus.RemoveRange(promotionMenus);
                }

                _db.FoodPromotions.Remove(promotion);
                _db.SaveChanges();

                TempData["SuccessMessage"] = "ลบโปรโมชั่นสำเร็จแล้ว";
                return RedirectToAction("ManagePromotion");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "ลบโปรโมชั่นไม่สำเร็จ: " + ex.Message;
                return RedirectToAction("ManagePromotion");
            }
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private void LoadPromotionPageData()
        {
            var allMenus = _db.FoodMenus
                .Include(m => m.MenuType)
                .OrderBy(m => m.MenuId)
                .ToList();

            var promotionMenus = _db.FoodPromotionMenus
                .ToList();

            ViewBag.Menus = allMenus;
            ViewBag.AllMenus = allMenus;
            ViewBag.PromotionMenus = promotionMenus;
        }

        private DateTime NormalizeThaiYear(DateTime dt)
        {
            // ถ้าปีเป็น พ.ศ. (เช่น 2568) ให้แปลงเป็น ค.ศ.
            if (dt.Year > DateTime.Now.Year + 100)
            {
                dt = dt.AddYears(-543);
            }

            return dt;
        }
    }
}