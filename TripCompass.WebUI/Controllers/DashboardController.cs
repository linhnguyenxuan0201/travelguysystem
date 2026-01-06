using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces.Repositories;

namespace TripCompass.WebUI.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IPostRepository _postRepository;
        private readonly ICurrentUserService _currentUser;

        public DashboardController(
            IPostRepository postRepository,
            ICurrentUserService currentUser)
        {
            _postRepository = postRepository;
            _currentUser = currentUser;
        }

        // =========================
        // STATISTICS PAGE
        // =========================
        public IActionResult Statistics()
        {
            return View();
        }

        // =========================
        // API FOR CHART
        // =========================
        [HttpGet]
        public async Task<IActionResult> MonthlyStats(int? year)
        {
            var userId = _currentUser.UserId;
            var selectedYear = year ?? DateTime.UtcNow.Year;

            var data = await _postRepository
                .GetMonthlyStatsAsync(userId, selectedYear);

            return Json(data);
        }

        // =========================
        // EXPORT EXCEL
        // =========================
        [HttpGet]
        public async Task<IActionResult> ExportStatisticsExcel(int year)
        {
            var userId = _currentUser.UserId;

            var data = await _postRepository
                .GetMonthlyStatsAsync(userId, year);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Statistics");

            // HEADER
            ws.Cell(1, 1).Value = "Month";
            ws.Cell(1, 2).Value = "Reviews";
            ws.Cell(1, 3).Value = "Views";
            ws.Cell(1, 4).Value = "Likes";

            ws.Range("A1:D1").Style.Font.Bold = true;

            int row = 2;
            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.Month;
                ws.Cell(row, 2).Value = x.ReviewCount;
                ws.Cell(row, 3).Value = x.ViewCount;
                ws.Cell(row, 4).Value = x.LikeCount;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"TripCompass_Statistics_{year}.xlsx"
            );
        }
    }
}
