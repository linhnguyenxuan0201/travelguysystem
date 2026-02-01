using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Categories.GetCategories
{
    public class GetCategoriesQuery : IRequest<List<CategoryListItemDto>>
    {
        public string? SearchTerm { get; set; }
    }
}
