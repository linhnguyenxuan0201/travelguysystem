using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Locations.GetLocations;
using TripCompass.Application.Features.Admin.Locations.MergeLocations;
using TripCompass.Application.Features.Admin.Locations.RenameLocation;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class LocationController : Controller
    {
        private readonly IMediator _mediator;

        public LocationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetLocationsQuery query)
        {
            var locations = await _mediator.Send(query);
            
            ViewBag.SearchTerm = query.SearchTerm;
            
            return View(locations);
        }

        [HttpPost("Merge")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Merge(MergeLocationsCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.SourceLocation) || string.IsNullOrWhiteSpace(command.TargetLocation))
            {
                TempData["Error"] = "Source and target locations are required";
                return RedirectToAction(nameof(Index));
            }

            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = $"Successfully merged '{command.SourceLocation}' into '{command.TargetLocation}'";
            }
            else
            {
                TempData["Error"] = "Failed to merge locations. Please check if locations exist and are different.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Rename")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(RenameLocationCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.OldLocation) || string.IsNullOrWhiteSpace(command.NewLocation))
            {
                TempData["Error"] = "Old and new location names are required";
                return RedirectToAction(nameof(Index));
            }

            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = $"Successfully renamed '{command.OldLocation}' to '{command.NewLocation}'";
            }
            else
            {
                TempData["Error"] = "Failed to rename location. Please check if location exists.";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
