using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Users.BanUser
{
    public class BanUserHandler : IRequestHandler<BanUserCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public BanUserHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(BanUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) return false;

            var adminId = _currentUser.UserId;
            if (adminId == 0) throw new UnauthorizedAccessException("Admin ID not found");

            user.Ban();
            await _context.SaveChangesAsync(cancellationToken);
            
            // Log action
            var adminLog = new AdminLog
            {
                AdminId = adminId,
                ActionType = "BAN_USER",
                TargetTable = "Users",
                TargetId = user.UserId,
                Note = $"Banned user {user.UserName}. Reason: {request.Reason ?? "No reason provided"}",
                CreatedAt = DateTime.UtcNow
            };
            _context.AdminLogs.Add(adminLog);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
