using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Users.BanUser;
using TripCompass.Application.Features.Admin.Users.GetUsers;
using TripCompass.Application.Features.Admin.Users.UnbanUser;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? status, string? role, int page = 1)
        {
            bool? isBanned = null;
            if (status == "banned") isBanned = true;
            else if (status == "active") isBanned = false;

            var query = new GetUsersQuery 
            { 
                SearchTerm = searchTerm, 
                IsBanned = isBanned,
                Role = role,
                Page = page, 
                PageSize = 10 
            };
            var (items, totalCount) = await _mediator.Send(query);

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Role = role;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 10.0);

            return View(items);
        }

        [HttpPost("Ban/{id}")]
        public async Task<IActionResult> Ban(long id, string reason)
        {
            var command = new BanUserCommand { UserId = id, Reason = reason };
            await _mediator.Send(command);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Unban/{id}")]
        public async Task<IActionResult> Unban(long id)
        {
            var command = new UnbanUserCommand { UserId = id };
            await _mediator.Send(command);
            return RedirectToAction(nameof(Index));
        }
    }
}
