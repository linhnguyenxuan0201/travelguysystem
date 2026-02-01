using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.AdPackages.GetAdPackages
{
    public class GetAdPackagesHandler : IRequestHandler<GetAdPackagesQuery, List<AdPackageListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetAdPackagesHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdPackageListItemDto>> Handle(GetAdPackagesQuery request, CancellationToken cancellationToken)
        {
            var query = from discountCode in _context.PartnerDiscountCodes
                       join partner in _context.Partners on discountCode.PartnerUserId equals partner.UserId
                       join user in _context.Users on partner.UserId equals user.UserId
                       select new { discountCode, partner, user };

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(x =>
                    x.discountCode.Code.ToLower().Contains(term) ||
                    x.user.UserName.ToLower().Contains(term) ||
                    x.user.Email.ToLower().Contains(term) ||
                    x.partner.StoreName.ToLower().Contains(term));
            }

            // Filter by IsActive
            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.discountCode.IsActive == request.IsActive.Value);
            }

            // Order By (Default: Newest first)
            query = query.OrderByDescending(x => x.discountCode.CreatedAt);

            var items = await query
                .Select(x => new AdPackageListItemDto
                {
                    PartnerDiscountCodeId = x.discountCode.PartnerDiscountCodeId,
                    PartnerUserId = x.discountCode.PartnerUserId,
                    PartnerName = x.partner.StoreName,
                    Code = x.discountCode.Code,
                    PercentOff = x.discountCode.PercentOff,
                    Purpose = x.discountCode.Purpose,
                    ExpiryDate = x.discountCode.ExpiryDate,
                    IsActive = x.discountCode.IsActive,
                    StatusDisplay = x.discountCode.IsActive ? "Active" : "Pending",
                    CreatedAt = x.discountCode.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
