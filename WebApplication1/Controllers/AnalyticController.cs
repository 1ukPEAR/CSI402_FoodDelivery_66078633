using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers
{
    public class AnalyticController : Controller
    {
        private readonly FooddeliverydbContext _db;

        public AnalyticController(FooddeliverydbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Analytic()
        {
            // =========================
            // โหลดข้อมูลที่หน้า Dashboard ใช้งานจริง
            // =========================
            var users = _db.FoodUsers.ToList();

            var reviews = _db.FoodReviews
                .OrderByDescending(r => r.ReviewDate)
                .ToList();

            var orders = _db.FoodOrders.ToList();

            var orderItems = _db.FoodOrderitems.ToList();

            var menus = _db.FoodMenus.ToList();

            // =========================
            // ส่งข้อมูลให้ View ผ่าน ViewBag
            // =========================
            ViewBag.Reviews = reviews;
            ViewBag.Users = users;
            ViewBag.Orders = orders;
            ViewBag.OrderItems = orderItems;
            ViewBag.Menus = menus;

            // คง model แบบเดิมไว้ เพื่อไม่ให้ View เดิมพัง
            return View(users);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}