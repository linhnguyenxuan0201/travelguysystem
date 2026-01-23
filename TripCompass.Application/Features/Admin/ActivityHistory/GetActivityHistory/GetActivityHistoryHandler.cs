using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.ActivityHistory.GetActivityHistory
{
    public class GetActivityHistoryHandler : IRequestHandler<GetActivityHistoryQuery, (List<ActivityHistoryDto> Items, int TotalCount)>
    {
        private readonly IApplicationDbContext _context;

        public GetActivityHistoryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<ActivityHistoryDto> Items, int TotalCount)> Handle(GetActivityHistoryQuery request, CancellationToken cancellationToken)
        {
            var query = _context.AdminLogs
                .Include(log => log.Admin)
                    .ThenInclude(admin => admin.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .AsQueryable();

            // Filter by AdminId
            if (request.AdminId.HasValue)
            {
                query = query.Where(log => log.AdminId == request.AdminId.Value);
            }

            // Filter by ActionType
            if (!string.IsNullOrEmpty(request.ActionType))
            {
                query = query.Where(log => log.ActionType == request.ActionType);
            }

            // Filter by TargetTable
            if (!string.IsNullOrEmpty(request.TargetTable))
            {
                query = query.Where(log => log.TargetTable == request.TargetTable);
            }

            // Filter by date range
            if (request.FromDate.HasValue)
            {
                query = query.Where(log => log.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(log => log.CreatedAt <= request.ToDate.Value.AddDays(1)); // Include the entire day
            }

            // Search term (search in Note, AdminName, ActionType)
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(log =>
                    (log.Note != null && log.Note.Contains(request.SearchTerm)) ||
                    log.Admin.UserName.Contains(request.SearchTerm) ||
                    log.Admin.Email.Contains(request.SearchTerm) ||
                    log.ActionType.Contains(request.SearchTerm));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(log => log.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var result = items.Select(log => new ActivityHistoryDto
            {
                LogId = log.LogId,
                AdminId = log.AdminId,
                AdminName = log.Admin.UserName,
                AdminEmail = log.Admin.Email,
                UserRole = log.Admin.UserRoles
                    .OrderBy(ur => ur.RoleId)
                    .Select(ur => ur.Role.RoleName)
                    .FirstOrDefault() ?? "User",
                ActionType = log.ActionType,
                TargetTable = log.TargetTable,
                TargetId = log.TargetId,
                Note = log.Note,
                IpAddress = log.IpAddress,
                CreatedAt = log.CreatedAt
            }).ToList();

            return (result, totalCount);
        }
    }
}
