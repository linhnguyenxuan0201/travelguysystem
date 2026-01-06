using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Common.Security;
using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Infrastructure;
using TripCompass.Infrastructure.Persistence;
using TripCompass.Infrastructure.Repositories;
using TripCompass.Infrastructure.Security;
using TripCompass.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

/* =========================
   ADD SERVICES
========================= */

// MVC
builder.Services.AddControllersWithViews();

/* =========================
   DATABASE
========================= */

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

/* =========================
   AUTHENTICATION + AUTHORIZATION
========================= */

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "TripCompassCookie";
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie("TripCompassCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId =
            builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret =
            builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
    });

builder.Services.AddAuthorization();

/* =========================
   DEPENDENCY INJECTION (CORE)
========================= */

// 👉 ĐÚNG KIẾN TRÚC: gọi Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// 👉 Application services (không thuộc Infrastructure)
builder.Services.AddScoped<LoginService>();

/* =========================
   BUILD APP
========================= */

var app = builder.Build();

/* =========================
   MIDDLEWARE PIPELINE
========================= */

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⚠️ BẮT BUỘC
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
