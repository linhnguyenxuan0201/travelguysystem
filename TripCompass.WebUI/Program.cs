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

// ✅ MEDIATR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Admin.Dashboard.GetDashboardStats.GetDashboardStatsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Admin.Users.GetUsers.GetUsersHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(TripCompass.Application.Features.Posts.CreatePost.CreatePostHandler).Assembly);
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

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
