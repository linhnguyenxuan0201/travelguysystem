using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Partners.GetPartners
{
    public class GetPartnersHandler : IRequestHandler<GetPartnersQuery, PaginatedList<PartnerListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPartnersHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<PartnerListItemDto>> Handle(GetPartnersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Partners
                .Include(p => p.User)
                .AsNoTracking()
                .AsQueryable();

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.StoreName.ToLower().Contains(term) ||
                    p.RepresentativeName.ToLower().Contains(term) ||
                    p.PhoneNumber.Contains(term) ||
                    p.BusinessAddress.ToLower().Contains(term) ||
                    p.User.UserName.ToLower().Contains(term) ||
                    p.User.Email.ToLower().Contains(term) ||
                    p.PartnerId.ToString() == term);
            }

            // Filter by IsApproved
            if (request.IsApproved.HasValue)
            {
                query = query.Where(p => p.IsApproved == request.IsApproved.Value);
            }

            // Filter by BusinessType
            if (!string.IsNullOrEmpty(request.BusinessType))
            {
                query = query.Where(p => p.BusinessType == request.BusinessType);
            }

            // Filter by Date
            if (request.FromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);
            }

            // Order By (Default: Newest first, Pending first)
            query = query.OrderByDescending(p => !p.IsApproved) // Pending first
                        .ThenByDescending(p => p.CreatedAt);

            // Get partners with pagination
            var partnersList = await PaginatedList<Domain.Entities.Partner>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

            // Project to DTOs
            var partnerDtos = partnersList.Items.Select(p => new PartnerListItemDto
            {
                PartnerId = p.PartnerId,
                UserId = p.UserId,
                UserName = p.User.UserName,
                UserEmail = p.User.Email,
                StoreName = p.StoreName,
                BusinessType = p.BusinessType,
                RepresentativeName = p.RepresentativeName,
                PhoneNumber = p.PhoneNumber,
                BusinessAddress = p.BusinessAddress,
                BankName = p.BankName,
                AccountNumber = p.AccountNumber,
                AccountHolderName = p.AccountHolderName,
                IdNumber = p.IdNumber,
                TaxId = p.TaxId,
                ServiceDescription = p.ServiceDescription,
                IsApproved = p.IsApproved,
                StatusDisplay = p.IsApproved ? "Approved" : "Pending",
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return new PaginatedList<PartnerListItemDto>(
                partnerDtos,
                partnersList.TotalCount,
                partnersList.PageNumber,
                request.PageSize);
        }
    }
}
