using MediatR;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Posts.ChangePostStatus
{
    public class ChangePostStatusCommand : IRequest<bool>
    {
        public long PostId { get; set; }
        public PostStatus? NewStatus { get; set; }
        public string? ModerationNote { get; set; }
        public bool? IsDeleted { get; set; } // For soft delete
    }
}
