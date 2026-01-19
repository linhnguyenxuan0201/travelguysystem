using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin")]
    [Authorize(Roles = "Admin")]
    public class PortalController : Controller
    {
        [HttpGet]
        [HttpGet("Portal")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
