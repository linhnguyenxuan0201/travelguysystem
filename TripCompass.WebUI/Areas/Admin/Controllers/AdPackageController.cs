using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.AdPackages.GetAdPackages;
using TripCompass.Application.Features.Admin.AdPackages.ApproveAdPackage;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdPackageController : Controller
    {
        private readonly IMediator _mediator;

        public AdPackageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetAdPackagesQuery query)
        {
            var packages = await _mediator.Send(query);
            
            ViewBag.SearchTerm = query.SearchTerm;
            ViewBag.SelectedIsActive = query.IsActive;
            
            return View(packages);
        }

        [HttpPost("Approve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long id)
        {
            var command = new ApproveAdPackageCommand { PartnerDiscountCodeId = id };
            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = "Ad package approved successfully";
            }
            else
            {
                TempData["Error"] = "Failed to approve ad package";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
