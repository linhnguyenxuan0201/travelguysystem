using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.ActivityHistory.GetActivityHistory;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ActivityHistoryController : Controller
    {
        private readonly IMediator _mediator;

        public ActivityHistoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetActivityHistoryQuery query)
        {
            var result = await _mediator.Send(query);
            
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.CurrentPage = query.Page;
            ViewBag.PageSize = query.PageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)query.PageSize);
            
            // Pass filter values back to view
            ViewBag.AdminId = query.AdminId;
            ViewBag.ActionType = query.ActionType;
            ViewBag.TargetTable = query.TargetTable;
            ViewBag.FromDate = query.FromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = query.ToDate?.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = query.SearchTerm;

            return View(result.Items);
        }
    }
}
