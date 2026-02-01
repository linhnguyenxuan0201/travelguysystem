using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Reports.GetReports;
using TripCompass.Application.Features.Admin.Reports.ResolveReport;
using TripCompass.Application.Features.Admin.Reports.RejectReport;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly IMediator _mediator;

        public ReportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetReportsQuery query)
        {
            var reports = await _mediator.Send(query);
            
            ViewBag.SelectedStatus = query.Status;
            ViewBag.SelectedTargetType = query.TargetType;
            ViewBag.SearchTerm = query.SearchTerm;
            ViewBag.FromDate = query.FromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = query.ToDate?.ToString("yyyy-MM-dd");
            
            return View(reports);
        }

        [HttpPost("Resolve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(long id, string? resolutionNote)
        {
            var command = new ResolveReportCommand 
            { 
                ReportId = id, 
                ResolutionNote = resolutionNote 
            };
            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = "Report resolved successfully";
            }
            else
            {
                TempData["Error"] = "Failed to resolve report";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Reject/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long id, string? rejectionNote)
        {
            var command = new RejectReportCommand 
            { 
                ReportId = id, 
                RejectionNote = rejectionNote 
            };
            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = "Report rejected successfully";
            }
            else
            {
                TempData["Error"] = "Failed to reject report";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
