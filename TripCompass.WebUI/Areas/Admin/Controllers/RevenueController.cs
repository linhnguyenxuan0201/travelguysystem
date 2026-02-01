using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Revenue.GetRevenueStats;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RevenueController : Controller
    {
        private readonly IMediator _mediator;

        public RevenueController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetRevenueStatsQuery query)
        {
            var stats = await _mediator.Send(query);
            
            ViewBag.FromDate = query.FromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = query.ToDate?.ToString("yyyy-MM-dd");
            
            return View(stats);
        }
    }
}
