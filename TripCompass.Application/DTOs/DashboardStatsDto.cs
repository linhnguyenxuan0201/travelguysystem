using System.Collections.Generic;

namespace TripCompass.Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int TotalPosts { get; set; }
        public int PendingPosts { get; set; }
        public int PendingReports { get; set; }
        public int TotalCoinBalance { get; set; }
        
        // UC-ADM-01 Additions
        public int BannedUsers { get; set; }
        public List<TopPostDto> TopViewedPosts { get; set; } = new();
        public List<ChartDataDto> UserGrowthData { get; set; } = new();
    }

    public class TopPostDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public int ViewCount { get; set; }
        public string AuthorName { get; set; } = null!;
    }

    public class ChartDataDto
    {
        public string Date { get; set; } = null!;
        public int Count { get; set; }
    }
}
