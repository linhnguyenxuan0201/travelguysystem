using MediatR;

namespace TripCompass.Application.Features.Admin.Categories.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<bool>
    {
        public long CategoryId { get; set; }
    }
}
