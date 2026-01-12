using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Users.UnbanUser
{
    public class UnbanUserHandler : IRequestHandler<UnbanUserCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public UnbanUserHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UnbanUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) return false;

            user.Unban();
            await _context.SaveChangesAsync(cancellationToken);

            // Log action
            var adminLog = new AdminLog
            {
                AdminId = 1, // TODO: Get current user ID
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
