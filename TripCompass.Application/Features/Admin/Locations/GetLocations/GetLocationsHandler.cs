using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Locations.GetLocations
{
    public class GetLocationsHandler : IRequestHandler<GetLocationsQuery, List<LocationListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetLocationsHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LocationListItemDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Posts
                .Where(p => !p.IsDeleted && !string.IsNullOrEmpty(p.Location))
                .AsNoTracking()
                .AsQueryable();

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(p => p.Location != null && p.Location.ToLower().Contains(term));
            }

            var locations = await query
                .GroupBy(p => p.Location!)
                .Select(g => new LocationListItemDto
                {
                    Location = g.Key,
                    PostCount = g.Count(),
                    TotalViews = g.Sum(p => p.ViewCount),
                    TotalLikes = g.Sum(p => p.LikeCount),
                    LastPostDate = g.Max(p => p.CreatedAt)
                })
                .OrderByDescending(l => l.PostCount)
                .ThenByDescending(l => l.LastPostDate)
                .ToListAsync(cancellationToken);

            return locations;
        }
    }
}
