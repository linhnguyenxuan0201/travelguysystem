using MediatR;

namespace TripCompass.Application.Features.Admin.Categories.CreateCategory
{
    public class CreateCategoryCommand : IRequest<long>
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Icon { get; set; }
    }
}
