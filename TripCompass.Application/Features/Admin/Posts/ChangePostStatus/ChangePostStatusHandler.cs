using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Posts.ChangePostStatus
{
    public class ChangePostStatusHandler : IRequestHandler<ChangePostStatusCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public ChangePostStatusHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ChangePostStatusCommand request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts.FindAsync(new object[] { request.PostId }, cancellationToken);

            if (post == null) return false;

            post.Status = request.NewStatus;
            
            if (!string.IsNullOrEmpty(request.ModerationNote))
            {
                post.ModerationNote = request.ModerationNote;
            }

            // If publishing, set PublishedAt
            if (request.NewStatus == PostStatus.Published && post.PublishedAt == null)
            {
                post.PublishedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
