using MediatR;
using Microsoft.EntityFrameworkCore;
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
            
            // Nếu là admin từ config (UserId = 0), tìm một admin user trong database để dùng cho logging
            if (adminId == 0 && _currentUser.IsConfigAdmin())
            {
                var adminUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Admin"))
                    .OrderBy(u => u.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (adminUser != null)
                {
                    adminId = adminUser.UserId;
                }
                else
                {
                    // Nếu không tìm thấy admin user nào, skip logging nhưng vẫn cho phép thực hiện action
                    adminId = 0;
                }
            }
            else if (adminId == 0)
            {
                throw new UnauthorizedAccessException("Admin ID not found");
            }

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

            // Log action (chỉ log nếu có adminId hợp lệ)
            if (adminId > 0)
            {
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
            }

            return true;
        }
    }
}
