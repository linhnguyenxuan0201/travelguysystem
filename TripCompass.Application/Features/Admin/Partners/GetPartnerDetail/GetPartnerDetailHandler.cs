using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Partners.GetPartnerDetail
{
    public class GetPartnerDetailHandler : IRequestHandler<GetPartnerDetailQuery, PartnerDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetPartnerDetailHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PartnerDetailDto> Handle(GetPartnerDetailQuery request, CancellationToken cancellationToken)
        {
            var partner = await _context.Partners
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PartnerId == request.PartnerId, cancellationToken);

            if (partner == null)
            {
                throw new KeyNotFoundException($"Partner with ID {request.PartnerId} not found");
            }

            var userId = partner.UserId;

            // Get Statistics
            var posts = await _context.Posts
                .Where(p => p.UserId == userId && p.IsPartner)
                .ToListAsync(cancellationToken);

            var bookings = await _context.PostBookings
                .Include(b => b.Post)
                .Where(b => b.PartnerUserId == userId)
                .ToListAsync(cancellationToken);

            var adPackages = await _context.PartnerDiscountCodes
                .Where(pdc => pdc.PartnerUserId == userId)
                .ToListAsync(cancellationToken);

            var statistics = new PartnerStatisticsDto
            {
                TotalPosts = posts.Count,
                PublishedPosts = posts.Count(p => p.Status == PostStatus.Published && !p.IsDeleted),
                PendingPosts = posts.Count(p => p.Status == PostStatus.Pending && !p.IsDeleted),
                TotalBookings = bookings.Count,
                CompletedBookings = bookings.Count(b => b.Status == "Completed"),
                ProcessingBookings = bookings.Count(b => b.Status == "Processing"),
                CancelledBookings = bookings.Count(b => b.Status == "Cancelled"),
                TotalRevenue = bookings.Where(b => b.PaymentStatus == "Paid").Sum(b => b.TotalAmount),
                TotalCommission = bookings.Where(b => b.CommissionDeducted && b.CommissionAmount.HasValue).Sum(b => b.CommissionAmount ?? 0),
                TotalAdPackages = adPackages.Count,
                ActiveAdPackages = adPackages.Count(ap => ap.IsActive && (ap.ExpiryDate == null || ap.ExpiryDate > DateTime.UtcNow)),
                TotalViews = posts.Sum(p => p.ViewCount),
                TotalLikes = posts.Sum(p => p.LikeCount)
            };

            // Get Posts
            var partnerPosts = posts.Select(p => new PartnerPostDto
            {
                PostId = p.PostId,
                Title = p.Title,
                Location = p.Location,
                Status = p.Status.ToString(),
                StatusDisplay = GetStatusDisplay(p.Status),
                ViewCount = p.ViewCount,
                LikeCount = p.LikeCount,
                CreatedAt = p.CreatedAt,
                PublishedAt = p.PublishedAt,
                IsDeleted = p.IsDeleted
            }).OrderByDescending(p => p.CreatedAt).ToList();

            // Get Bookings
            var partnerBookings = bookings.Select(b => new PartnerBookingDto
            {
                BookingId = b.BookingId,
                PostId = b.PostId,
                PostTitle = b.Post.Title,
                CustomerUserId = b.CustomerUserId,
                CustomerName = b.CustomerName,
                CustomerPhone = b.CustomerPhone,
                BookedAt = b.BookedAt,
                VisitDate = b.VisitDate,
                Quantity = b.Quantity,
                TotalAmount = b.TotalAmount,
                Status = b.Status,
                PaymentStatus = b.PaymentStatus,
                PaymentMethod = b.PaymentMethod,
                CommissionAmount = b.CommissionAmount
            }).OrderByDescending(b => b.BookedAt).ToList();

            // Get Ad Packages
            var partnerAdPackages = adPackages.Select(ap => new PartnerAdPackageDto
            {
                PartnerDiscountCodeId = ap.PartnerDiscountCodeId,
                Code = ap.Code,
                PercentOff = ap.PercentOff,
                Purpose = ap.Purpose,
                ExpiryDate = ap.ExpiryDate,
                IsActive = ap.IsActive,
                StatusDisplay = ap.IsActive && (ap.ExpiryDate == null || ap.ExpiryDate > DateTime.UtcNow) ? "Active" : "Inactive",
                CreatedAt = ap.CreatedAt
            }).OrderByDescending(ap => ap.CreatedAt).ToList();

            return new PartnerDetailDto
            {
                PartnerId = partner.PartnerId,
                UserId = partner.UserId,
                UserName = partner.User.UserName,
                UserEmail = partner.User.Email,
                UserCreatedAt = partner.User.CreatedAt,
                UserReputationScore = partner.User.ReputationScore,
                UserReputationLevel = partner.User.ReputationLevel,
                StoreName = partner.StoreName,
                BusinessType = partner.BusinessType,
                RepresentativeName = partner.RepresentativeName,
                PhoneNumber = partner.PhoneNumber,
                BusinessAddress = partner.BusinessAddress,
                BankName = partner.BankName,
                AccountNumber = partner.AccountNumber,
                AccountHolderName = partner.AccountHolderName,
                IdNumber = partner.IdNumber,
                TaxId = partner.TaxId,
                ServiceDescription = partner.ServiceDescription,
                IsApproved = partner.IsApproved,
                StatusDisplay = partner.IsApproved ? "Approved" : "Pending",
                CreatedAt = partner.CreatedAt,
                UpdatedAt = partner.UpdatedAt,
                Statistics = statistics,
                Posts = partnerPosts,
                Bookings = partnerBookings,
                AdPackages = partnerAdPackages
            };
        }

        private string GetStatusDisplay(PostStatus status)
        {
            return status switch
            {
                PostStatus.Draft => "Draft",
                PostStatus.Pending => "Pending",
                PostStatus.Published => "Published",
                PostStatus.Rejected => "Rejected",
                _ => status.ToString()
            };
        }
    }
}
