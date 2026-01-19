using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Posts.ChangePostStatus;
using TripCompass.Application.Features.Admin.Posts.GetPostById;
using TripCompass.Application.Features.Admin.Posts.GetPosts;
using TripCompass.Application.Features.Admin.Posts.UpdatePost;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
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
            ViewBag.SelectedStatus = query.Status; // Pass selected status to view
            ViewBag.SearchTerm = query.SearchTerm; // Pass search term to view
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
            try
            {
                // Parse NewStatus from form (can be null if only IsDeleted is changed)
                if (Request.Form.ContainsKey("NewStatus") && !string.IsNullOrEmpty(Request.Form["NewStatus"]))
                {
                    if (int.TryParse(Request.Form["NewStatus"], out int statusValue))
                    {
                        command.NewStatus = (TripCompass.Domain.Enums.PostStatus)statusValue;
                    }
                }

                // Parse IsDeleted from form
                // Checkbox sends "true" when checked, "false" when unchecked (restore), or nothing if not present
                if (Request.Form.ContainsKey("IsDeleted"))
                {
                    var isDeletedValue = Request.Form["IsDeleted"].ToString();
                    command.IsDeleted = isDeletedValue == "true" ? true : 
                                       isDeletedValue == "false" ? false : 
                                       (bool?)null;
                }

                var result = await _mediator.Send(command);
                if (!result) return NotFound();

                TempData["Success"] = "Post status updated successfully";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating post status: " + ex.Message;
            }
            
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
