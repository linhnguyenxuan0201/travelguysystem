using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Locations.RenameLocation
{
    public class RenameLocationHandler : IRequestHandler<RenameLocationCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IMediator _mediator;

        public RenameLocationHandler(IApplicationDbContext context, ICurrentUserService currentUser, IMediator mediator)
        {
            _context = context;
            _currentUser = currentUser;
            _mediator = mediator;
        }

        public async Task<bool> Handle(RenameLocationCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.OldLocation) || 
                string.IsNullOrWhiteSpace(request.NewLocation) ||
                request.OldLocation.Equals(request.NewLocation, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Check if new location already exists
            var existingPosts = await _context.Posts
                .Where(p => p.Location != null && p.Location.Equals(request.NewLocation, StringComparison.OrdinalIgnoreCase))
                .AnyAsync(cancellationToken);

            if (existingPosts)
            {
                // If exists, merge instead
                var mergeCommand = new MergeLocations.MergeLocationsCommand
                {
                    SourceLocation = request.OldLocation,
                    TargetLocation = request.NewLocation
                };
                return await _mediator.Send(mergeCommand, cancellationToken);
            }

            // Update all posts with old location to new location
            var postsToUpdate = await _context.Posts
                .Where(p => p.Location != null && p.Location.Equals(request.OldLocation, StringComparison.OrdinalIgnoreCase))
                .ToListAsync(cancellationToken);

            if (!postsToUpdate.Any())
            {
                return false;
            }

            foreach (var post in postsToUpdate)
            {
                post.Location = request.NewLocation;
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
                    ActionType = "RENAME_LOCATION",
                    TargetTable = "Posts",
                    TargetId = 0,
                    Note = $"Renamed location '{request.OldLocation}' to '{request.NewLocation}'. Updated {postsToUpdate.Count} posts.",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
