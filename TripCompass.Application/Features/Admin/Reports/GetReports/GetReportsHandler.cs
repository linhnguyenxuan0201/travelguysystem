using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Reports.GetReports
{
    public class GetReportsHandler : IRequestHandler<GetReportsQuery, PaginatedList<ReportListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetReportsHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<ReportListItemDto>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.Resolver)
                .AsNoTracking()
                .AsQueryable();

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(r =>
                    r.Reason.ToLower().Contains(term) ||
                    (r.Description != null && r.Description.ToLower().Contains(term)) ||
                    r.Reporter.UserName.ToLower().Contains(term) ||
                    r.Reporter.Email.ToLower().Contains(term) ||
                    r.ReportId.ToString() == term);
            }

            // Filter by Status
            if (request.Status.HasValue)
            {
                query = query.Where(r => r.Status == request.Status.Value);
            }

            // Filter by TargetType
            if (!string.IsNullOrEmpty(request.TargetType))
            {
                query = query.Where(r => r.TargetType == request.TargetType);
            }

            // Filter by Date
            if (request.FromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= request.ToDate.Value);
            }

            // Order By (Default: Newest first, Pending first)
            query = query.OrderByDescending(r => r.Status == 0) // Pending first
                        .ThenByDescending(r => r.CreatedAt);

            // Get reports with pagination
            var reportsList = await PaginatedList<Report>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

            // Get target details for POST and USER types
            var postIds = reportsList.Items
                .Where(r => r.TargetType == "POST")
                .Select(r => r.TargetId)
                .Distinct()
                .ToList();

            var userIds = reportsList.Items
                .Where(r => r.TargetType == "USER")
                .Select(r => r.TargetId)
                .Distinct()
                .ToList();

            var posts = await _context.Posts
                .Where(p => postIds.Contains(p.PostId))
                .Select(p => new { p.PostId, p.Title })
                .ToListAsync(cancellationToken);

            var targetUsers = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.UserName })
                .ToListAsync(cancellationToken);

            // Project to DTOs
            var reportDtos = reportsList.Items.Select(r =>
            {
                var dto = new ReportListItemDto
                {
                    ReportId = r.ReportId,
                    ReporterId = r.ReporterId,
                    ReporterName = r.Reporter.UserName,
                    ReporterEmail = r.Reporter.Email,
                    TargetId = r.TargetId,
                    TargetType = r.TargetType,
                    Reason = r.Reason,
                    Description = r.Description,
                    Status = r.Status,
                    StatusDisplay = r.Status == 0 ? "Pending" : r.Status == 1 ? "Resolved" : "Rejected",
                    ResolvedBy = r.ResolvedBy,
                    ResolverName = r.Resolver?.UserName,
                    ResolvedAt = r.ResolvedAt,
                    CreatedAt = r.CreatedAt
                };

                // Add target details
                if (r.TargetType == "POST")
                {
                    dto.TargetTitle = posts.FirstOrDefault(p => p.PostId == r.TargetId)?.Title;
                }
                else if (r.TargetType == "USER")
                {
                    dto.TargetUserName = targetUsers.FirstOrDefault(u => u.UserId == r.TargetId)?.UserName;
                }

                return dto;
            }).ToList();

            return new PaginatedList<ReportListItemDto>(
                reportDtos,
                reportsList.TotalCount,
                reportsList.PageNumber,
                request.PageSize);
        }
    }
}
