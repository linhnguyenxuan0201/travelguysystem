using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Comments.DeleteComment
{
    public class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteCommentHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = await _context.PostComments
                .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);
            
            if (comment == null) return false;

            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.PostId == comment.PostId, cancellationToken);
            
            var postTitle = post?.Title ?? "Unknown";

            var adminId = _currentUser.UserId;
            
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
                    adminId = 0;
                }
            }
            else if (adminId == 0)
            {
                throw new UnauthorizedAccessException("Admin ID not found");
            }

            comment.Delete();
            await _context.SaveChangesAsync(cancellationToken);

            // Log action
            if (adminId > 0)
            {
                var adminLog = new AdminLog
                {
                    AdminId = adminId,
                    ActionType = "DELETE_COMMENT",
                    TargetTable = "PostComments",
                    TargetId = comment.Id,
                    Note = $"Deleted comment on post: {postTitle}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
