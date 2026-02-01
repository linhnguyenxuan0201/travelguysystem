using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.AdPackages.ApproveAdPackage
{
    public class ApproveAdPackageHandler : IRequestHandler<ApproveAdPackageCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ApproveAdPackageHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(ApproveAdPackageCommand request, CancellationToken cancellationToken)
        {
            var discountCode = await _context.PartnerDiscountCodes
                .FirstOrDefaultAsync(d => d.PartnerDiscountCodeId == request.PartnerDiscountCodeId, cancellationToken);
            
            if (discountCode == null) return false;

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
            else if (adminId == 0)
            {
                throw new UnauthorizedAccessException("Admin ID not found");
            }

            discountCode.IsActive = true;
            await _context.SaveChangesAsync(cancellationToken);

            // Log action
            if (adminId > 0)
            {
                var adminLog = new AdminLog
                {
                    AdminId = adminId,
                    ActionType = "APPROVE_AD_PACKAGE",
                    TargetTable = "PartnerDiscountCodes",
                    TargetId = discountCode.PartnerDiscountCodeId,
                    Note = $"Approved ad package: {discountCode.Code}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
