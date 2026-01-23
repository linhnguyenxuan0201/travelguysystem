using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Users.GetUsers
{
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, (List<UserListItemDto> Items, int TotalCount)>
    {
        private readonly IApplicationDbContext _context;

        public GetUsersHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<UserListItemDto> Items, int TotalCount)> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(u => u.UserName.Contains(request.SearchTerm) || u.Email.Contains(request.SearchTerm));
            }

            if (request.IsBanned.HasValue)
            {
                query = query.Where(u => u.IsBanned == request.IsBanned.Value);
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == request.Role));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserListItemDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Email = u.Email,
                    Role = u.UserRoles
                        .OrderBy(ur => ur.RoleId)
                        .Select(ur => ur.Role.RoleName)
                        .FirstOrDefault() ?? string.Empty,
                    Status = u.IsBanned ? "Banned" : "Active",
                    ReputationScore = u.ReputationScore,
                    IsBanned = u.IsBanned,
                    CreatedAt = u.CreatedAt,
                    Roles = string.Join(", ", u.UserRoles.Select(ur => ur.Role.RoleName))
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
