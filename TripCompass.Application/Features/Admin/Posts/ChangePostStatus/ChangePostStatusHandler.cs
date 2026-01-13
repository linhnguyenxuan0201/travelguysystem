using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common;
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

            // Xử lý soft delete (Deleted có thể từ bất kỳ status nào)
            if (request.IsDeleted.HasValue)
            {
                post.IsDeleted = request.IsDeleted.Value;
                if (request.IsDeleted.Value)
                {
                    post.DeletedAt = DateTime.UtcNow;
                }
                else
                {
                    post.DeletedAt = null;
                }
            }

            // Nếu có NewStatus, validate và update
            if (request.NewStatus.HasValue)
            {
                // Validate workflow transition
                if (!PostStatusWorkflow.IsValidTransition(post.Status, request.NewStatus.Value))
                {
                    throw new InvalidOperationException(
                        $"Invalid status transition from {post.Status} to {request.NewStatus.Value}. " +
                        $"Valid transitions: {string.Join(", ", PostStatusWorkflow.GetValidNextStatuses(post.Status))}");
                }

                // Không cho phép Published trực tiếp từ Draft
                if (post.Status == PostStatus.Draft && request.NewStatus.Value == PostStatus.Published)
                {
                    throw new InvalidOperationException(
                        "Cannot publish directly from Draft. Post must go through PendingApproval first.");
                }

                post.Status = request.NewStatus.Value;
                
                // If publishing, set PublishedAt
                if (request.NewStatus.Value == PostStatus.Published && post.PublishedAt == null)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }
            }
            
            if (!string.IsNullOrEmpty(request.ModerationNote))
            {
                post.ModerationNote = request.ModerationNote;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
