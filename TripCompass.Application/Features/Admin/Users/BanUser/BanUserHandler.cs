using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Users.BanUser
{
    public class BanUserHandler : IRequestHandler<BanUserCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public BanUserHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(BanUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) return false;

            user.Ban();
            await _context.SaveChangesAsync(cancellationToken);
            
            // Log action
            var adminLog = new AdminLog
            {
                AdminId = 1, // TODO: Get current user ID
                ActionType = "BAN_USER",
                TargetTable = "Users",
                TargetId = user.UserId,
                Note = $"Banned user {user.UserName}. Reason: {request.Reason}",
                CreatedAt = DateTime.UtcNow
            };
            _context.AdminLogs.Add(adminLog);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
