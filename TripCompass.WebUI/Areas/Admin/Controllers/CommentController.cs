using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Comments.GetComments;
using TripCompass.Application.Features.Admin.Comments.DeleteComment;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CommentController : Controller
    {
        private readonly IMediator _mediator;

        public CommentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetCommentsQuery query)
        {
            var comments = await _mediator.Send(query);
            
            ViewBag.SearchTerm = query.SearchTerm;
            ViewBag.PostId = query.PostId;
            ViewBag.UserId = query.UserId;
            ViewBag.IsDeleted = query.IsDeleted;
            ViewBag.FromDate = query.FromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = query.ToDate?.ToString("yyyy-MM-dd");
            
            return View(comments);
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var command = new DeleteCommentCommand { CommentId = id };
            var result = await _mediator.Send(command);
            
            if (result)
            {
                TempData["Success"] = "Comment deleted successfully";
            }
            else
            {
                TempData["Error"] = "Failed to delete comment";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
