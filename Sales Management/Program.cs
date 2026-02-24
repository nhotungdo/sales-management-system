using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Sales_Management.Data;
using Sales_Management.Services;
using Sales_Management.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Kết nối Database
var connectionString = builder.Configuration.GetConnectionString("DBDefault");
builder.Services.AddDbContext<SalesManagementContext>(options =>
    options.UseSqlServer(connectionString));

// Cấu hình Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

// Đăng ký các dịch vụ Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICoinService, CoinService>();

// Cấu hình Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký Background Services (Chạy ngầm)
builder.Services.AddHostedService<VoucherExpirationService>();
builder.Services.AddHostedService<CartAutoReleaseService>();

var app = builder.Build();

// 2. Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware kiểm tra Session cho Sales
app.UseMiddleware<Sales_Management.Middleware.SalesSessionMiddleware>();

// Session
app.UseSession();

// Cấu hình Routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapHub<SystemHub>("/systemHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();