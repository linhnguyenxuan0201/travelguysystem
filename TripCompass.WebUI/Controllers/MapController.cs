using Microsoft.AspNetCore.Mvc;

namespace TripCompass.WebUI.Controllers
{
    public class MapController : Controller
    {
        [HttpGet]
        public IActionResult Nearby()
        {
            return View();
        }
    }
}

