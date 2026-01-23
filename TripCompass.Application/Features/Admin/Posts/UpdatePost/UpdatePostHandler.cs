using MediatR;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Posts.UpdatePost
{
    public class UpdatePostHandler : IRequestHandler<UpdatePostCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdatePostHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts.FindAsync(new object[] { request.PostId }, cancellationToken);

            if (post == null) return false;

            var adminId = _currentUser.UserId;
            if (adminId == 0) throw new UnauthorizedAccessException("Admin ID not found");

            post.Title = request.Title;
            post.Content = request.Content;
            
            // SEO
            post.Slug = request.Slug;
            post.SeoTitle = request.SeoTitle;
            post.MetaDescription = request.MetaDescription;
            post.CanonicalUrl = request.CanonicalUrl;
            post.IsIndexable = request.IsIndexable;

            // Flags
            post.IsFeatured = request.IsFeatured;
            post.IsTrending = request.IsTrending;
            post.IsPinned = request.IsPinned;

            await _context.SaveChangesAsync(cancellationToken);

            // Log action
            var adminLog = new AdminLog
            {
                AdminId = adminId,
                ActionType = "UPDATE_POST",
                TargetTable = "Posts",
                TargetId = post.PostId,
                Note = $"Updated post: {post.Title}",
                CreatedAt = DateTime.UtcNow
            };
            _context.AdminLogs.Add(adminLog);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
