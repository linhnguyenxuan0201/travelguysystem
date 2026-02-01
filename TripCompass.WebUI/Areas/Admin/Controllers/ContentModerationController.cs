using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Domain.Enums;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ContentModerationController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Redirect to Content management with Pending filter
            return RedirectToAction("Index", "Content", new { area = "Admin", Status = (int)PostStatus.Pending });
        }
    }
}
