using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Security.UpdateSecuritySettings
{
    public class UpdateSecuritySettingsHandler : IRequestHandler<UpdateSecuritySettingsCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateSecuritySettingsHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(UpdateSecuritySettingsCommand request, CancellationToken cancellationToken)
        {
            // In production, these settings would be saved to database
            // For now, we'll just log the action

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

            // Log action
            if (adminId > 0)
            {
                var adminLog = new AdminLog
                {
                    AdminId = adminId,
                    ActionType = "UPDATE_SECURITY_SETTINGS",
                    TargetTable = "SystemSettings",
                    TargetId = 0,
                    Note = "Security settings updated",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
