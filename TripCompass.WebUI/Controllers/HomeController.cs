using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TripCompass.WebUI.Models;

namespace TripCompass.WebUI.Controllers
{
    public class HomeController : Controller
    {
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
