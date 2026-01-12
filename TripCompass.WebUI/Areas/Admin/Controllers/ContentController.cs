using MediatR;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Posts.ChangePostStatus;
using TripCompass.Application.Features.Admin.Posts.GetPostById;
using TripCompass.Application.Features.Admin.Posts.GetPosts;
using TripCompass.Application.Features.Admin.Posts.UpdatePost;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class ContentController : Controller
    {
        private readonly IMediator _mediator;

        public ContentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetPostsQuery query)
        {
            var posts = await _mediator.Send(query);
            return View(posts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(long id)
        {
            var post = await _mediator.Send(new GetPostByIdQuery(id));
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(long id)
        {
            var post = await _mediator.Send(new GetPostByIdQuery(id));
            if (post == null) return NotFound();
            
            // Map to command for editing
            var command = new UpdatePostCommand
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                Slug = post.Slug,
                SeoTitle = post.SeoTitle,
                MetaDescription = post.MetaDescription,
                CanonicalUrl = post.CanonicalUrl,
                IsIndexable = post.IsIndexable,
                IsFeatured = post.IsFeatured,
                IsTrending = post.IsTrending,
                IsPinned = post.IsPinned
            };
            
            return View(command);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UpdatePostCommand command)
        {
            if (!ModelState.IsValid) return View(command);

            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            TempData["Success"] = "Post updated successfully";
            return RedirectToAction(nameof(Details), new { id = command.PostId });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(ChangePostStatusCommand command, string? returnUrl = null)
        {
            var result = await _mediator.Send(command);
            if (!result) return NotFound();

            TempData["Success"] = "Post status updated successfully";
            
            // If returnUrl is provided (from Index), redirect back to Index
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            // Otherwise, redirect to Details (default behavior)
            return RedirectToAction(nameof(Details), new { id = command.PostId });
        }
    }
}
