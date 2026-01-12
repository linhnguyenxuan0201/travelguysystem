using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Dashboard.GetDashboardStats
{
    public class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetDashboardStatsHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;

            var totalUsers = await _context.Users.CountAsync(cancellationToken);
            var newUsers = await _context.Users.CountAsync(u => u.CreatedAt >= today, cancellationToken);
            
            var totalPosts = await _context.Posts.CountAsync(p => !p.IsDeleted, cancellationToken);
            var pendingPosts = await _context.Posts.CountAsync(p => !p.IsDeleted && p.Status == PostStatus.Pending, cancellationToken);
            
            var pendingReports = await _context.Reports.CountAsync(r => r.Status == 0, cancellationToken); // 0 = Pending
            
            var totalBalance = await _context.Wallets.SumAsync(w => w.Balance, cancellationToken);

            // UC-ADM-01: Banned Users
            var bannedUsers = await _context.Users.CountAsync(u => u.IsBanned, cancellationToken);

            // UC-ADM-01: Top 5 Viewed Posts
            var topPosts = await _context.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.ViewCount)
                .Take(5)
                .Select(p => new TopPostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    ViewCount = p.ViewCount,
                    AuthorName = "Unknown" // Need Include User to get name, doing below
                })
                .ToListAsync(cancellationToken);
            
            // Fix Author Name (or use Include above)
            // Let's optimize by using Select with navigation property if possible. 
            // Since I didn't include User in the query above, let's rewrite the query slightly.
            
            var topPostsWithAuthor = await _context.Posts
                .Include(p => p.User) // Assuming navigation property exists
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.ViewCount)
                .Take(5)
                .Select(p => new TopPostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    ViewCount = p.ViewCount,
                    AuthorName = p.User.UserName
                })
                .ToListAsync(cancellationToken);


            // UC-ADM-01: User Growth (Last 7 days)
            var sevenDaysAgo = today.AddDays(-6);
            var userGrowth = await _context.Users
                .Where(u => u.CreatedAt >= sevenDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new ChartDataDto
                {
                    Date = g.Key.ToString("dd/MM"),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);
            
            // Fill missing dates with 0
            var chartData = new List<ChartDataDto>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i).ToString("dd/MM");
                var existing = userGrowth.FirstOrDefault(x => x.Date == date);
                chartData.Add(new ChartDataDto 
                { 
                    Date = date, 
                    Count = existing?.Count ?? 0 
                });
            }

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                NewUsersToday = newUsers,
                TotalPosts = totalPosts,
                PendingPosts = pendingPosts,
                PendingReports = pendingReports,
                TotalCoinBalance = totalBalance,
                BannedUsers = bannedUsers,
                TopViewedPosts = topPostsWithAuthor,
                UserGrowthData = chartData
            };
        }
    }
}
