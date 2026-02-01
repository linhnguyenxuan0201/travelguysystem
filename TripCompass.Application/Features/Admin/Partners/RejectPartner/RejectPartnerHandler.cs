using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Partners.RejectPartner
{
    public class RejectPartnerHandler : IRequestHandler<RejectPartnerCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public RejectPartnerHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(RejectPartnerCommand request, CancellationToken cancellationToken)
        {
            var partner = await _context.Partners.FindAsync(new object[] { request.PartnerId }, cancellationToken);
            if (partner == null) return false;

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
                    adminId = 0;
                }
            }
            else if (adminId == 0)
            {
                throw new UnauthorizedAccessException("Admin ID not found");
            }

            partner.IsApproved = false;
            partner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Log action (chỉ log nếu có adminId hợp lệ)
            if (adminId > 0)
            {
                var note = $"Rejected partner #{partner.PartnerId} - {partner.StoreName}";
                if (!string.IsNullOrEmpty(request.RejectionNote))
                {
                    note += $". Note: {request.RejectionNote}";
                }

                var adminLog = new AdminLog
                {
                    AdminId = adminId,
                    ActionType = "REJECT_PARTNER",
                    TargetTable = "Partners",
                    TargetId = partner.PartnerId,
                    Note = note,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
