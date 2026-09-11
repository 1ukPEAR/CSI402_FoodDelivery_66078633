using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.FoodViewModels;
using WebApplication1.Models;
using WebApplication1.Models.db;

namespace WebApplication1.Controllers;

public class AuthController : Controller
{
    private readonly FooddeliverydbContext _db;
    private readonly PasswordHasher<FoodUser> _passwordHasher;

    public AuthController(FooddeliverydbContext db)
    {
        _db = db;
        _passwordHasher = new PasswordHasher<FoodUser>();
    }

    // =========================
    // REGISTER (ลูกค้า)
    // =========================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(AuthViewModel data)
    {
        if (string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
        {
            TempData["Error"] = "กรุณากรอก Username และ Password";
            return View(data);
        }

        bool usernameExists = _db.FoodUsers.Any(u => u.UserName == data.Username.Trim());
        if (usernameExists)
        {
            TempData["Error"] = "Username นี้ถูกใช้งานแล้ว";
            return View(data);
        }

        var lastUser = _db.FoodUsers
            .AsEnumerable()
            .Where(u => !string.IsNullOrEmpty(u.UserId) && u.UserId.StartsWith("U"))
            .OrderByDescending(u => u.UserId)
            .FirstOrDefault();

        int next = 1;
        if (lastUser != null && int.TryParse(lastUser.UserId.Substring(1), out int lastNum))
        {
            next = lastNum + 1;
        }

        string newUserId = "U" + next.ToString("D3");

        var user = new FoodUser
        {
            UserId = newUserId,
            RoleId = "R001", // CUSTOMER
            UserName = data.Username.Trim(),
            UserEmail = string.IsNullOrWhiteSpace(data.UserEmail) ? null : data.UserEmail.Trim(),
            UserPhone = string.IsNullOrWhiteSpace(data.UserPhone) ? null : data.UserPhone.Trim(),
            UserStatus = 1,
            RegisterDate = DateTime.Now,
            LoyaltyPoints = 0,
            OrderCount = 0
        };

        // ✅ HASH PASSWORD ก่อนบันทึก
        user.UserPassword = _passwordHasher.HashPassword(user, data.Password);

        _db.FoodUsers.Add(user);

        // สร้าง Address ถ้ามีข้อมูลครบ
        bool hasAddress =
            !string.IsNullOrWhiteSpace(data.AddressDetail) &&
            !string.IsNullOrWhiteSpace(data.SubDistrict) &&
            !string.IsNullOrWhiteSpace(data.District) &&
            !string.IsNullOrWhiteSpace(data.Province);

        if (hasAddress)
        {
            var lastAddr = _db.FoodAddresses
                .AsEnumerable()
                .Where(a => !string.IsNullOrEmpty(a.AddressId) && a.AddressId.StartsWith("ADDR"))
                .OrderByDescending(a => a.AddressId)
                .FirstOrDefault();

            int nextAddr = 1;
            if (lastAddr != null && int.TryParse(lastAddr.AddressId.Substring(4), out int lastAddrNum))
            {
                nextAddr = lastAddrNum + 1;
            }

            string newAddressId = "ADDR" + nextAddr.ToString("D3");

            var address = new FoodAddress
            {
                AddressId = newAddressId,
                UserId = newUserId,
                UserName = data.Username.Trim(),
                ReceiverPhone = string.IsNullOrWhiteSpace(data.ReceiverPhone)
                    ? (string.IsNullOrWhiteSpace(data.UserPhone) ? "" : data.UserPhone.Trim())
                    : data.ReceiverPhone.Trim(),
                AddressDetail = data.AddressDetail!.Trim(),
                SubDistrict = data.SubDistrict!.Trim(),
                District = data.District!.Trim(),
                Province = data.Province!.Trim()
            };

            _db.FoodAddresses.Add(address);
        }

        _db.SaveChanges();

        TempData["Success"] = "สมัครสมาชิกเรียบร้อยแล้ว กรุณาเข้าสู่ระบบ";
        return RedirectToAction("Login");
    }

    // =========================
    // LOGIN
    // =========================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AuthViewModel data)
    {
        if (string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
        {
            TempData["Error"] = "กรุณากรอก Username และ Password";
            return View(data);
        }

        var user = _db.FoodUsers
            .FirstOrDefault(u =>
                u.UserName == data.Username &&
                u.UserStatus == 1);

        if (user == null)
        {
            TempData["Error"] = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";
            return View(data);
        }

        bool isPasswordValid = false;
        string storedPassword = user.UserPassword ?? "";

        // 1) ถ้าเป็น hash ของ ASP.NET Identity -> verify แบบ hash
        bool looksLikeAspNetIdentityHash =
            !string.IsNullOrWhiteSpace(storedPassword) &&
            storedPassword.StartsWith("AQAAAA", StringComparison.Ordinal);

        if (looksLikeAspNetIdentityHash)
        {
            try
            {
                var verifyResult = _passwordHasher.VerifyHashedPassword(user, storedPassword, data.Password);

                if (verifyResult == PasswordVerificationResult.Success ||
                    verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    isPasswordValid = true;

                    if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        user.UserPassword = _passwordHasher.HashPassword(user, data.Password);
                        _db.SaveChanges();
                    }
                }
            }
            catch
            {
                isPasswordValid = false;
            }
        }

        // 2) รองรับ user เก่าแบบ plain text
        if (!isPasswordValid)
        {
            if (storedPassword == data.Password)
            {
                isPasswordValid = true;

                // auto upgrade เป็น hash ทันที
                user.UserPassword = _passwordHasher.HashPassword(user, data.Password);
                _db.SaveChanges();
            }
        }

        if (!isPasswordValid)
        {
            TempData["Error"] = "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง";
            return View(data);
        }

        string roleName = user.RoleId switch
        {
            "R001" => "CUSTOMER",
            "R002" => "EMPLOYEE",
            "R003" => "OWNER",
            _ => "CUSTOMER"
        };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("RoleId", user.RoleId ?? ""),
            new Claim("RoleName", roleName)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );

        if (roleName == "OWNER")
        {
            return RedirectToAction("ManageMenu", "Menu");
        }
        else if (roleName == "EMPLOYEE")
        {
            return RedirectToAction("OrderList", "Order");
        }
        else
        {
            return RedirectToAction("MenuUser", "Menu");
        }
    }

    // =========================
    // FORGOT PASSWORD (CUSTOMER ONLY)
    // =========================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(string username, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["Error"] = "กรุณากรอกข้อมูลให้ครบ";
            return View();
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "รหัสผ่านใหม่และยืนยันรหัสผ่านไม่ตรงกัน";
            return View();
        }

        if (newPassword.Length < 4)
        {
            TempData["Error"] = "รหัสผ่านใหม่ต้องมีอย่างน้อย 4 ตัวอักษร";
            return View();
        }

        var user = _db.FoodUsers.FirstOrDefault(u =>
            u.UserName == username.Trim() &&
            u.UserStatus == 1);

        if (user == null)
        {
            TempData["Error"] = "ไม่พบชื่อผู้ใช้นี้ในระบบ";
            return View();
        }

        // อนุญาตเฉพาะ CUSTOMER เท่านั้น
        if (user.RoleId != "R001")
        {
            TempData["Error"] = "บัญชีนี้ไม่สามารถเปลี่ยนรหัสผ่านผ่านหน้านี้ได้";
            return View();
        }

        // ✅ HASH PASSWORD ใหม่ก่อนบันทึก
        user.UserPassword = _passwordHasher.HashPassword(user, newPassword);
        _db.SaveChanges();

        TempData["Success"] = "เปลี่ยนรหัสผ่านเรียบร้อยแล้ว กรุณาเข้าสู่ระบบใหม่";
        return RedirectToAction("Login");
    }

    // =========================
    // LOGOUT
    // =========================
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();

        return RedirectToAction("Login", "Auth");
    }

    // =========================
    // ACCESS DENIED
    // =========================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
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