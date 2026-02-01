using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Partners.GetPartners;
using TripCompass.Application.Features.Admin.Partners.GetPartnerDetail;
using TripCompass.Application.Features.Admin.Partners.ApprovePartner;
using TripCompass.Application.Features.Admin.Partners.RejectPartner;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PartnerController : Controller
    {
        private readonly IMediator _mediator;

        public PartnerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetPartnersQuery query)
        {
            var partners = await _mediator.Send(query);
            
            ViewBag.SelectedIsApproved = query.IsApproved;
            ViewBag.SelectedBusinessType = query.BusinessType;
            ViewBag.SearchTerm = query.SearchTerm;
            ViewBag.FromDate = query.FromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = query.ToDate?.ToString("yyyy-MM-dd");
            
            return View(partners);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(long id)
        {
            try
            {
                var query = new GetPartnerDetailQuery { PartnerId = id };
                var partnerDetail = await _mediator.Send(query);
                return View(partnerDetail);
            }
            catch (KeyNotFoundException)
            {
                TempData["Error"] = "Partner not found";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("Approve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long id, string? approvalNote)
        {
            var command = new ApprovePartnerCommand 
            { 
                PartnerId = id, 
                ApprovalNote = approvalNote 
            };
            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = "Partner approved successfully";
            }
            else
            {
                TempData["Error"] = "Failed to approve partner";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Reject/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long id, string? rejectionNote)
        {
            var command = new RejectPartnerCommand 
            { 
                PartnerId = id, 
                RejectionNote = rejectionNote 
            };
            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = "Partner rejected successfully";
            }
            else
            {
                TempData["Error"] = "Failed to reject partner";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
