using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.CoinTransactions.GetCoinTransactions;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CoinTransactionController : Controller
    {
        private readonly IMediator _mediator;

        public CoinTransactionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetCoinTransactionsQuery query)
        {
            var transactions = await _mediator.Send(query);
            
            ViewBag.SearchTerm = query.SearchTerm;
            ViewBag.SelectedType = query.Type;
            ViewBag.UserId = query.UserId;
            ViewBag.FromDate = query.FromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = query.ToDate?.ToString("yyyy-MM-dd");
            
            return View(transactions);
        }
    }
}
