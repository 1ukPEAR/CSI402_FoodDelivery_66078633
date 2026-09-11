using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.FoodViewModels;
using WebApplication1.Models;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly FooddeliverydbContext _db;
    private readonly PasswordHasher<FoodUser> _passwordHasher;

    public UserController(FooddeliverydbContext db)
    {
        _db = db;
        _passwordHasher = new PasswordHasher<FoodUser>();
    }

    // =========================
    // DTOs สำหรับ AJAX (Customer Popup)
    // =========================
    public class UpdateProfileRequest
    {
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }
    }

    public class AddAddressRequest
    {
        public string? UserName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? AddressDetail { get; set; }
        public string? SubDistrict { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
    }

    public class UpdateAddressRequest
    {
        public string? AddressId { get; set; }
        public string? UserName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? AddressDetail { get; set; }
        public string? SubDistrict { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
    }

    public class DeleteAddressRequest
    {
        public string? AddressId { get; set; }
    }

    // =========================================================
    // OWNER SECTION
    // =========================================================

    // แสดงรายการผู้ใช้ทั้งหมด
    [Authorize(Roles = "OWNER")]
    [HttpGet]
    public IActionResult UserList()
    {
        var users = (from u in _db.FoodUsers
                     select new UserViewModel
                     {
                         UserId = u.UserId,
                         UserName = u.UserName,
                         UserEmail = u.UserEmail,
                         UserPhone = u.UserPhone,
                         UserStatus = u.UserStatus,
                         RegisterDate = u.RegisterDate,
                         RoleId = u.RoleId
                     }).ToList();

        return View(users);
    }

    // =========================
    // CREATE EMPLOYEE
    // =========================
    [Authorize(Roles = "OWNER")]
    [HttpGet]
    public IActionResult UserCreate()
    {
        return View();
    }

    [Authorize(Roles = "OWNER")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UserCreate(AuthViewModel data)
    {
        if (string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
        {
            TempData["Error"] = "กรุณากรอก Username และ Password";
            return RedirectToAction("UserList");
        }

        bool usernameExists = _db.FoodUsers.Any(u => u.UserName == data.Username.Trim());
        if (usernameExists)
        {
            TempData["Error"] = "Username นี้มีอยู่แล้ว";
            return RedirectToAction("UserList");
        }

        var allUserIds = _db.FoodUsers
            .AsEnumerable()
            .Select(u => u.UserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        int maxNumber = 0;

        foreach (var id in allUserIds)
        {
            string numberPart = new string(id.Where(char.IsDigit).ToArray());

            if (!string.IsNullOrWhiteSpace(numberPart) && int.TryParse(numberPart, out int num))
            {
                if (num > maxNumber)
                    maxNumber = num;
            }
        }

        int next = maxNumber + 1;
        string newUserId = "U" + next.ToString("D3");

        var employee = new FoodUser
        {
            UserId = newUserId,
            RoleId = "R002", // Employee only
            UserName = data.Username.Trim(),
            UserEmail = string.IsNullOrWhiteSpace(data.UserEmail) ? null : data.UserEmail.Trim(),
            UserPhone = string.IsNullOrWhiteSpace(data.UserPhone) ? null : data.UserPhone.Trim(),
            UserStatus = 1,
            RegisterDate = DateTime.Now,
            LoyaltyPoints = 0,
            OrderCount = 0
        };

        // ✅ HASH PASSWORD ก่อนบันทึก
        employee.UserPassword = _passwordHasher.HashPassword(employee, data.Password);

        _db.FoodUsers.Add(employee);
        _db.SaveChanges();

        TempData["Success"] = "สร้างบัญชีพนักงานเรียบร้อยแล้ว";
        return RedirectToAction("UserList");
    }

    // =========================
    // EDIT EMPLOYEE (OWNER ONLY)
    // =========================
    [Authorize(Roles = "OWNER")]
    [HttpGet]
    public IActionResult UserEdit(string id)
    {
        var user = _db.FoodUsers.FirstOrDefault(u => u.UserId == id);

        if (user == null)
        {
            TempData["Error"] = "ไม่พบข้อมูลผู้ใช้";
            return RedirectToAction("UserList");
        }

        // อนุญาตให้แก้เฉพาะพนักงานเท่านั้น
        if (user.RoleId != "R002")
        {
            TempData["Error"] = "หน้านี้ใช้แก้ไขข้อมูลพนักงานเท่านั้น";
            return RedirectToAction("UserList");
        }

        return View(user);
    }

    [Authorize(Roles = "OWNER")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UserEdit(FoodUser data)
    {
        var user = _db.FoodUsers.FirstOrDefault(u => u.UserId == data.UserId);

        if (user == null)
        {
            TempData["Error"] = "ไม่พบข้อมูลผู้ใช้";
            return RedirectToAction("UserList");
        }

        // อนุญาตให้แก้เฉพาะพนักงานเท่านั้น
        if (user.RoleId != "R002")
        {
            TempData["Error"] = "หน้านี้ใช้แก้ไขข้อมูลพนักงานเท่านั้น";
            return RedirectToAction("UserList");
        }

        if (string.IsNullOrWhiteSpace(data.UserName))
        {
            TempData["Error"] = "กรุณากรอก Username";
            return RedirectToAction("UserEdit", new { id = data.UserId });
        }

        bool usernameExists = _db.FoodUsers.Any(u =>
            u.UserName == data.UserName.Trim() &&
            u.UserId != data.UserId);

        if (usernameExists)
        {
            TempData["Error"] = "Username นี้มีอยู่แล้ว";
            return RedirectToAction("UserEdit", new { id = data.UserId });
        }

        // อัปเดตข้อมูลพื้นฐาน
        user.UserName = data.UserName.Trim();
        user.UserEmail = string.IsNullOrWhiteSpace(data.UserEmail) ? null : data.UserEmail.Trim();
        user.UserPhone = string.IsNullOrWhiteSpace(data.UserPhone) ? null : data.UserPhone.Trim();
        user.UserStatus = data.UserStatus ?? 1;

        // =========================
        // ถ้ามีการกรอกรหัสใหม่ -> เปลี่ยนรหัส + hash
        // =========================
        bool hasNewPassword = !string.IsNullOrWhiteSpace(data.NewPassword) ||
                              !string.IsNullOrWhiteSpace(data.ConfirmPassword);

        if (hasNewPassword)
        {
            if (string.IsNullOrWhiteSpace(data.NewPassword) || string.IsNullOrWhiteSpace(data.ConfirmPassword))
            {
                TempData["Error"] = "กรุณากรอกรหัสผ่านใหม่และยืนยันรหัสผ่านให้ครบ";
                return RedirectToAction("UserEdit", new { id = data.UserId });
            }

            if (data.NewPassword != data.ConfirmPassword)
            {
                TempData["Error"] = "รหัสผ่านใหม่และยืนยันรหัสผ่านไม่ตรงกัน";
                return RedirectToAction("UserEdit", new { id = data.UserId });
            }

            if (data.NewPassword.Length < 4)
            {
                TempData["Error"] = "รหัสผ่านใหม่ต้องมีอย่างน้อย 4 ตัวอักษร";
                return RedirectToAction("UserEdit", new { id = data.UserId });
            }

            // ✅ HASH PASSWORD ก่อนบันทึก
            user.UserPassword = _passwordHasher.HashPassword(user, data.NewPassword);
        }

        _db.SaveChanges();

        TempData["Success"] = hasNewPassword
            ? "แก้ไขข้อมูลพนักงานและเปลี่ยนรหัสผ่านเรียบร้อยแล้ว"
            : "แก้ไขข้อมูลพนักงานเรียบร้อยแล้ว";

        return RedirectToAction("UserList");
    }

    // =========================
    // DELETE EMPLOYEE ONLY
    // =========================
    [Authorize(Roles = "OWNER")]
    [HttpGet]
    public IActionResult UserDelete(string id)
    {
        var user = _db.FoodUsers.FirstOrDefault(u => u.UserId == id);

        if (user == null)
        {
            TempData["Error"] = "ไม่พบข้อมูลผู้ใช้";
            return RedirectToAction("UserList");
        }

        // อนุญาตให้ลบเฉพาะพนักงานเท่านั้น
        if (user.RoleId != "R002")
        {
            TempData["Error"] = "สามารถลบได้เฉพาะบัญชีพนักงานเท่านั้น";
            return RedirectToAction("UserList");
        }

        // ลบ address ที่เกี่ยวข้องก่อน (ปกติ employee อาจไม่มี แต่กันไว้)
        var addresses = _db.FoodAddresses.Where(a => a.UserId == id).ToList();
        if (addresses.Any())
        {
            _db.FoodAddresses.RemoveRange(addresses);
        }

        _db.FoodUsers.Remove(user);
        _db.SaveChanges();

        TempData["Success"] = "ลบบัญชีพนักงานเรียบร้อยแล้ว";
        return RedirectToAction("UserList");
    }
    // =========================================================
    // OWNER PROFILE SECTION
    // =========================================================

    // หน้าโปรไฟล์ของเจ้าของร้าน
    [Authorize(Roles = "OWNER")]
    [HttpGet]
    public IActionResult OwnerProfile()
    {
        string? ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งาน";
            return RedirectToAction("Login", "Auth");
        }

        var owner = _db.FoodUsers.FirstOrDefault(u => u.UserId == ownerId);

        if (owner == null)
        {
            TempData["Error"] = "ไม่พบข้อมูลเจ้าของร้าน";
            return RedirectToAction("UserList");
        }

        // กันพลาด: หน้านี้ใช้เฉพาะ OWNER
        if (owner.RoleId != "R003")
        {
            TempData["Error"] = "ไม่มีสิทธิ์เข้าถึงหน้านี้";
            return RedirectToAction("UserList");
        }

        return View(owner);
    }

    // บันทึกโปรไฟล์ของเจ้าของร้าน
    [Authorize(Roles = "OWNER")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult OwnerProfile(FoodUser data)
    {
        string? ownerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            TempData["Error"] = "ไม่พบข้อมูลผู้ใช้งาน";
            return RedirectToAction("Login", "Auth");
        }

        var owner = _db.FoodUsers.FirstOrDefault(u => u.UserId == ownerId);

        if (owner == null)
        {
            TempData["Error"] = "ไม่พบข้อมูลเจ้าของร้าน";
            return RedirectToAction("UserList");
        }

        // กันพลาด: หน้านี้ใช้เฉพาะ OWNER
        if (owner.RoleId != "R003")
        {
            TempData["Error"] = "ไม่มีสิทธิ์เข้าถึงหน้านี้";
            return RedirectToAction("UserList");
        }

        if (string.IsNullOrWhiteSpace(data.UserName))
        {
            TempData["Error"] = "กรุณากรอก Username";
            return RedirectToAction("OwnerProfile");
        }

        string newUserName = data.UserName.Trim();

        bool usernameExists = _db.FoodUsers.Any(u =>
            u.UserName == newUserName &&
            u.UserId != ownerId);

        if (usernameExists)
        {
            TempData["Error"] = "Username นี้มีอยู่แล้ว";
            return RedirectToAction("OwnerProfile");
        }

        // อัปเดตข้อมูลพื้นฐาน
        owner.UserName = newUserName;
        owner.UserEmail = string.IsNullOrWhiteSpace(data.UserEmail) ? null : data.UserEmail.Trim();
        owner.UserPhone = string.IsNullOrWhiteSpace(data.UserPhone) ? null : data.UserPhone.Trim();

        // =========================
        // ถ้ามีการกรอกรหัสใหม่ -> เปลี่ยนรหัส + hash
        // =========================
        bool hasNewPassword = !string.IsNullOrWhiteSpace(data.NewPassword) ||
                              !string.IsNullOrWhiteSpace(data.ConfirmPassword);

        if (hasNewPassword)
        {
            if (string.IsNullOrWhiteSpace(data.NewPassword) || string.IsNullOrWhiteSpace(data.ConfirmPassword))
            {
                TempData["Error"] = "กรุณากรอกรหัสผ่านใหม่และยืนยันรหัสผ่านให้ครบ";
                return RedirectToAction("OwnerProfile");
            }

            if (data.NewPassword != data.ConfirmPassword)
            {
                TempData["Error"] = "รหัสผ่านใหม่และยืนยันรหัสผ่านไม่ตรงกัน";
                return RedirectToAction("OwnerProfile");
            }

            if (data.NewPassword.Length < 4)
            {
                TempData["Error"] = "รหัสผ่านใหม่ต้องมีอย่างน้อย 4 ตัวอักษร";
                return RedirectToAction("OwnerProfile");
            }

            // ✅ HASH PASSWORD ก่อนบันทึก
            owner.UserPassword = _passwordHasher.HashPassword(owner, data.NewPassword);
        }

        _db.SaveChanges();

        TempData["Success"] = hasNewPassword
            ? "บันทึกโปรไฟล์และเปลี่ยนรหัสผ่านเรียบร้อยแล้ว"
            : "บันทึกโปรไฟล์เรียบร้อยแล้ว";

        return RedirectToAction("OwnerProfile");
    }

    // =========================================================
    // CUSTOMER SECTION (Popup ใน Navbar)
    // =========================================================

    // โหลดข้อมูลโปรไฟล์ลูกค้า
    [Authorize(Roles = "CUSTOMER")]
    [HttpGet]
    public IActionResult GetProfile()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        var user = _db.FoodUsers.FirstOrDefault(u => u.UserId == userId);

        if (user == null)
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        return Json(new
        {
            success = true,
            userName = user.UserName,
            userEmail = user.UserEmail,
            userPhone = user.UserPhone
        });
    }

    // บันทึกโปรไฟล์ลูกค้า (แก้ได้เฉพาะ Username / Email / Phone)
    [Authorize(Roles = "CUSTOMER")]
    [HttpPost]
    public IActionResult UpdateProfileAjax([FromBody] UpdateProfileRequest model)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        var user = _db.FoodUsers.FirstOrDefault(u => u.UserId == userId);

        if (user == null)
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        if (string.IsNullOrWhiteSpace(model.UserName))
        {
            return Json(new
            {
                success = false,
                message = "กรุณากรอก Username"
            });
        }

        string newUserName = model.UserName.Trim();

        bool usernameExists = _db.FoodUsers.Any(u =>
            u.UserName == newUserName &&
            u.UserId != userId);

        if (usernameExists)
        {
            return Json(new
            {
                success = false,
                message = "Username นี้ถูกใช้งานแล้ว"
            });
        }

        user.UserName = newUserName;
        user.UserEmail = string.IsNullOrWhiteSpace(model.UserEmail) ? null : model.UserEmail.Trim();
        user.UserPhone = string.IsNullOrWhiteSpace(model.UserPhone) ? null : model.UserPhone.Trim();

        _db.SaveChanges();

        return Json(new
        {
            success = true,
            message = "บันทึกข้อมูลโปรไฟล์เรียบร้อยแล้ว"
        });
    }

    // โหลดรายการที่อยู่ของลูกค้า
    [Authorize(Roles = "CUSTOMER")]
    [HttpGet]
    public IActionResult GetAddresses()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        var addresses = _db.FoodAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AddressId)
            .Select(a => new
            {
                a.AddressId,
                a.UserId,
                a.UserName,
                a.ReceiverPhone,
                a.AddressDetail,
                a.SubDistrict,
                a.District,
                a.Province
            })
            .ToList();

        return Json(new
        {
            success = true,
            addresses = addresses
        });
    }

    // เพิ่มที่อยู่ใหม่ของลูกค้า
    [Authorize(Roles = "CUSTOMER")]
    [HttpPost]
    public IActionResult AddAddressAjax([FromBody] AddAddressRequest model)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        if (string.IsNullOrWhiteSpace(model.UserName) ||
            string.IsNullOrWhiteSpace(model.ReceiverPhone) ||
            string.IsNullOrWhiteSpace(model.AddressDetail) ||
            string.IsNullOrWhiteSpace(model.SubDistrict) ||
            string.IsNullOrWhiteSpace(model.District) ||
            string.IsNullOrWhiteSpace(model.Province))
        {
            return Json(new
            {
                success = false,
                message = "กรุณากรอกข้อมูลที่อยู่ให้ครบ"
            });
        }

        string newAddressId = GenerateNextAddressId();

        var address = new FoodAddress
        {
            AddressId = newAddressId,
            UserId = userId,
            UserName = model.UserName.Trim(),
            ReceiverPhone = model.ReceiverPhone.Trim(),
            AddressDetail = model.AddressDetail.Trim(),
            SubDistrict = model.SubDistrict.Trim(),
            District = model.District.Trim(),
            Province = model.Province.Trim()
        };

        _db.FoodAddresses.Add(address);
        _db.SaveChanges();

        return Json(new
        {
            success = true,
            message = "เพิ่มที่อยู่ใหม่เรียบร้อยแล้ว"
        });
    }

    // แก้ไขที่อยู่ของลูกค้า
    [Authorize(Roles = "CUSTOMER")]
    [HttpPost]
    public IActionResult UpdateAddressAjax([FromBody] UpdateAddressRequest model)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        if (string.IsNullOrWhiteSpace(model.AddressId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบรหัสที่อยู่"
            });
        }

        if (string.IsNullOrWhiteSpace(model.UserName) ||
            string.IsNullOrWhiteSpace(model.ReceiverPhone) ||
            string.IsNullOrWhiteSpace(model.AddressDetail) ||
            string.IsNullOrWhiteSpace(model.SubDistrict) ||
            string.IsNullOrWhiteSpace(model.District) ||
            string.IsNullOrWhiteSpace(model.Province))
        {
            return Json(new
            {
                success = false,
                message = "กรุณากรอกข้อมูลที่อยู่ให้ครบ"
            });
        }

        var address = _db.FoodAddresses
            .FirstOrDefault(a => a.AddressId == model.AddressId && a.UserId == userId);

        if (address == null)
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบที่อยู่ที่ต้องการแก้ไข"
            });
        }

        address.UserName = model.UserName.Trim();
        address.ReceiverPhone = model.ReceiverPhone.Trim();
        address.AddressDetail = model.AddressDetail.Trim();
        address.SubDistrict = model.SubDistrict.Trim();
        address.District = model.District.Trim();
        address.Province = model.Province.Trim();

        _db.SaveChanges();

        return Json(new
        {
            success = true,
            message = "แก้ไขที่อยู่เรียบร้อยแล้ว"
        });
    }

    // ลบที่อยู่ของลูกค้า
    [Authorize(Roles = "CUSTOMER")]
    [HttpPost]
    public IActionResult DeleteAddressAjax([FromBody] DeleteAddressRequest model)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบข้อมูลผู้ใช้งาน"
            });
        }

        if (string.IsNullOrWhiteSpace(model.AddressId))
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบรหัสที่อยู่"
            });
        }

        var address = _db.FoodAddresses
            .FirstOrDefault(a => a.AddressId == model.AddressId && a.UserId == userId);

        if (address == null)
        {
            return Json(new
            {
                success = false,
                message = "ไม่พบที่อยู่ที่ต้องการลบ"
            });
        }

        _db.FoodAddresses.Remove(address);
        _db.SaveChanges();

        return Json(new
        {
            success = true,
            message = "ลบที่อยู่เรียบร้อยแล้ว"
        });
    }

    // =========================
    // HELPER : Generate AddressId
    // บังคับให้เป็น ADDR001, ADDR002...
    // =========================
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
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}