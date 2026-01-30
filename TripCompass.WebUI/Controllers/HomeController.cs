using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using TripCompass.Infrastructure.Persistence;
using TripCompass.WebUI.Models;
using TripCompass.WebUI.ViewModels;

namespace TripCompass.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // Nếu là Admin và đã đăng nhập, tự động điều hướng sang Admin Portal
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Portal", new { area = "Admin" });
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Premium()
        {
            var vm = new PremiumViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false
            };

            if (vm.IsAuthenticated)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    var user = await _db.Users
                        .FirstOrDefaultAsync(u => u.Email == email);

                    if (user != null)
                    {
                        var currentPlan = await _db.UserPlans
                            .Where(x => x.UserId == user.UserId && (x.ExpiredAt == null || x.ExpiredAt > DateTime.UtcNow))
                            .OrderByDescending(x => x.StartedAt)
                            .FirstOrDefaultAsync();

                        vm.CurrentPlan = currentPlan?.PlanCode ?? "Free";
                        vm.IsPremium = vm.CurrentPlan == "Pro" || vm.CurrentPlan == "Enterprise";
                        vm.PremiumExpiresAt = currentPlan?.ExpiredAt;
                    }
                }
            }
            else
            {
                vm.CurrentPlan = "Free";
            }

            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
