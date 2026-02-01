using MediatR;

namespace TripCompass.Application.Features.Admin.Categories.UpdateCategory
{
    public class UpdateCategoryCommand : IRequest<bool>
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Icon { get; set; }
    }
}
