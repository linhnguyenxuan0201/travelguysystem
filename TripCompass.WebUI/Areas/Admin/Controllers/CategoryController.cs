using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Categories.GetCategories;
using TripCompass.Application.Features.Admin.Categories.CreateCategory;
using TripCompass.Application.Features.Admin.Categories.UpdateCategory;
using TripCompass.Application.Features.Admin.Categories.DeleteCategory;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly IMediator _mediator;

        public CategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index(GetCategoriesQuery query)
        {
            var categories = await _mediator.Send(query);
            
            ViewBag.SearchTerm = query.SearchTerm;
            
            return View(categories);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryCommand command)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid category data";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var categoryId = await _mediator.Send(command);
                TempData["Success"] = "Category created successfully";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating category: " + ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Update/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(long id, UpdateCategoryCommand command)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid category data";
                return RedirectToAction(nameof(Index));
            }

            command.CategoryId = id;

            try
            {
                var result = await _mediator.Send(command);
                if (result)
                {
                    TempData["Success"] = "Category updated successfully";
                }
                else
                {
                    TempData["Error"] = "Category not found";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating category: " + ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var command = new DeleteCategoryCommand { CategoryId = id };
                var result = await _mediator.Send(command);
                
                if (result)
                {
                    TempData["Success"] = "Category deleted successfully";
                }
                else
                {
                    TempData["Error"] = "Category not found";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting category: " + ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
