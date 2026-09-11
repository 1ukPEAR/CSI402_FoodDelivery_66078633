using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.db;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<FooddeliverydbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";                   // ยังไม่ login
        options.AccessDeniedPath = "/Auth/AccessDenied";     // login แล้วแต่ role ไม่ตรง
        options.LogoutPath = "/Auth/Logout";                 // ระบุ path สำหรับ logout (optional)
        options.ExpireTimeSpan = TimeSpan.FromHours(2);      // อายุ cookie 2 ชม.
        options.SlidingExpiration = true;                    // ใช้งานต่อเนื่องให้ต่อเวลาอัตโนมัติ
    });

// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⭐ ลำดับสำคัญมาก
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();