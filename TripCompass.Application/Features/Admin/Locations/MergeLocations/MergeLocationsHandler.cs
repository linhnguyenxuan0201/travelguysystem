using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Locations.MergeLocations
{
    public class MergeLocationsHandler : IRequestHandler<MergeLocationsCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public MergeLocationsHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(MergeLocationsCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SourceLocation) || 
                string.IsNullOrWhiteSpace(request.TargetLocation) ||
                request.SourceLocation.Equals(request.TargetLocation, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Update all posts with source location to target location
            var postsToUpdate = await _context.Posts
                .Where(p => p.Location != null && p.Location.Equals(request.SourceLocation, StringComparison.OrdinalIgnoreCase))
                .ToListAsync(cancellationToken);

            if (!postsToUpdate.Any())
            {
                return false;
            }

            foreach (var post in postsToUpdate)
            {
                post.Location = request.TargetLocation;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Log action
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
            }

            if (adminId > 0)
            {
                var adminLog = new AdminLog
                {
                    AdminId = adminId,
                    ActionType = "MERGE_LOCATIONS",
                    TargetTable = "Posts",
                    TargetId = 0,
                    Note = $"Merged location '{request.SourceLocation}' into '{request.TargetLocation}'. Updated {postsToUpdate.Count} posts.",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
