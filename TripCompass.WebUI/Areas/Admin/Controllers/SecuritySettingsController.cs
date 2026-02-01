using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Security.GetSecuritySettings;
using TripCompass.Application.Features.Admin.Security.UpdateSecuritySettings;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SecuritySettingsController : Controller
    {
        private readonly IMediator _mediator;

        public SecuritySettingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _mediator.Send(new GetSecuritySettingsQuery());
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UpdateSecuritySettingsCommand command)
        {
            if (!ModelState.IsValid)
            {
                var settings = await _mediator.Send(new GetSecuritySettingsQuery());
                return View(settings);
            }

            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = "Security settings updated successfully";
            }
            else
            {
                TempData["Error"] = "Failed to update security settings";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
