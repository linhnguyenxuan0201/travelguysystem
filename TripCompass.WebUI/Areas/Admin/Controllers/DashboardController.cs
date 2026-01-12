using MediatR;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Features.Admin.Dashboard.GetDashboardStats;

namespace TripCompass.WebUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class DashboardController : Controller
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var query = new GetDashboardStatsQuery();
            var stats = await _mediator.Send(query);
            return View(stats);
        }

        [HttpGet("ExportReport")]
        public async Task<IActionResult> ExportReport()
        {
            var query = new GetDashboardStatsQuery();
            var stats = await _mediator.Send(query);

            // Create CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("TripCompass Admin Dashboard Report");
            csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();
            csv.AppendLine("=== STATISTICS ===");
            csv.AppendLine($"Total Users,{stats.TotalUsers}");
            csv.AppendLine($"New Users Today,{stats.NewUsersToday}");
            csv.AppendLine($"Banned Users,{stats.BannedUsers}");
            csv.AppendLine($"Total Posts,{stats.TotalPosts}");
            csv.AppendLine($"Pending Posts,{stats.PendingPosts}");
            csv.AppendLine($"Pending Reports,{stats.PendingReports}");
            csv.AppendLine($"Total Coin Balance,{stats.TotalCoinBalance}");
            csv.AppendLine();
            csv.AppendLine("=== TOP VIEWED POSTS ===");
            csv.AppendLine("Title,Author,Views");
            foreach (var post in stats.TopViewedPosts)
            {
                csv.AppendLine($"\"{post.Title}\",{post.AuthorName},{post.ViewCount}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"dashboard-report-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            
            return File(bytes, "text/csv", fileName);
        }
    }
}
