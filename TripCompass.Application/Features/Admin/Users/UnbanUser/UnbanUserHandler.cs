using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Users.UnbanUser
{
    public class UnbanUserHandler : IRequestHandler<UnbanUserCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UnbanUserHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(UnbanUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) return false;

            var adminId = _currentUser.UserId;
            if (adminId == 0) throw new UnauthorizedAccessException("Admin ID not found");

            user.Unban();
            await _context.SaveChangesAsync(cancellationToken);

            // Log action
            var adminLog = new AdminLog
            {
                AdminId = adminId,
                ActionType = "UNBAN_USER",
                TargetTable = "Users",
                TargetId = user.UserId,
                Note = $"Unbanned user {user.UserName}",
                CreatedAt = DateTime.UtcNow
            };
            _context.AdminLogs.Add(adminLog);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
