using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using TripCompass.Application.Auth;
using TripCompass.Application.Common.Security;
using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Infrastructure;
using TripCompass.Infrastructure.Persistence;
using TripCompass.Infrastructure.Repositories;
using TripCompass.Infrastructure.Security;
using TripCompass.Infrastructure.Services;
using TripCompass.WebUI.Hubs;
using TripCompass.WebUI.Services;
using TripCompass.WebUI.Services.Gemini;

var builder = WebApplication.CreateBuilder(args);

/* =========================
   ADD SERVICES
========================= */

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Gemini (LLM)
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<IGeminiClient, GeminiClient>();

/* =========================
   DATABASE
========================= */

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            // Tránh lỗi timeout/temporary network issues khi SQL Server phản hồi chậm
            sql.CommandTimeout(60);
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

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

        // Lưu ý: Không revoke cookie khi user bị ban, vì yêu cầu là vẫn cho đăng nhập
        // và hiển thị banner "tài khoản bị khóa" trong UI.
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
builder.Services.AddScoped<TripCompass.Application.Interfaces.INotificationRealtimeService, SignalRNotificationRealtimeService>();
builder.Services.AddScoped<TripCompass.Application.Interfaces.IChatRealtimeService, SignalRChatRealtimeService>();

// 👉 Admin Config từ appsettings.json
builder.Services.Configure<TripCompass.Application.Common.Security.AdminConfig>(
    builder.Configuration.GetSection("Authentication:Admin"));

// ✅ MEDIATR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Admin.Dashboard.GetDashboardStats.GetDashboardStatsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Admin.Users.GetUsers.GetUsersHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Admin.ActivityHistory.GetActivityHistory.GetActivityHistoryHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Posts.CreatePost.CreatePostHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Comments.CreateComment.CreateCommentHandler).Assembly);
});

/* =========================
   BUILD APP
========================= */

var app = builder.Build();

// SEED DATA
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // context.Database.EnsureCreated(); // Use with caution
        await DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

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

// Lưu ý: Không sign-out/redirect khi user bị ban, vì yêu cầu là vẫn cho đăng nhập
// và hiển thị banner "tài khoản bị khóa" trong UI.

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
