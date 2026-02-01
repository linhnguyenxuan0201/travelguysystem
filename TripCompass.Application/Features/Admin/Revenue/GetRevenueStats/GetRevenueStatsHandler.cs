using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Revenue.GetRevenueStats
{
    public class GetRevenueStatsHandler : IRequestHandler<GetRevenueStatsQuery, RevenueStatsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetRevenueStatsHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueStatsDto> Handle(GetRevenueStatsQuery request, CancellationToken cancellationToken)
        {
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-12);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            // Coin Transactions Stats
            var coinQuery = _context.CoinTransactions
                .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate);

            var totalCoinTransactions = await coinQuery
                .Where(t => t.Type == "PURCHASED" && t.Amount > 0)
                .SumAsync(t => (decimal)t.Amount, cancellationToken);

            var totalCoinTransactionsCount = await coinQuery
                .Where(t => t.Type == "PURCHASED")
                .CountAsync(cancellationToken);

            // Premium Orders Stats (if exists in future)
            // For now, we'll use 0 as PremiumOrder is not in DbContext yet
            var totalPremiumOrders = 0m;
            var totalPremiumOrdersCount = 0;

            var totalRevenue = totalCoinTransactions + totalPremiumOrders;

            // Daily Revenue (Last 30 days)
            var dailyRevenue = new List<DailyRevenueDto>();
            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
            {
                var dayCoin = await _context.CoinTransactions
                    .Where(t => t.CreatedAt.Date == date && t.Type == "PURCHASED" && t.Amount > 0)
                    .SumAsync(t => (decimal)t.Amount, cancellationToken);

                dailyRevenue.Add(new DailyRevenueDto
                {
                    Date = date.ToString("dd/MM/yyyy"),
                    CoinAmount = dayCoin,
                    PremiumAmount = 0m,
                    Total = dayCoin
                });
            }

            // Monthly Revenue (Last 12 months)
            var monthlyRevenue = new List<MonthlyRevenueDto>();
            var startMonth = DateTime.UtcNow.AddMonths(-12);
            for (var month = startMonth; month <= DateTime.UtcNow; month = month.AddMonths(1))
            {
                var monthStart = new DateTime(month.Year, month.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthCoin = await _context.CoinTransactions
                    .Where(t => t.CreatedAt >= monthStart && t.CreatedAt <= monthEnd && t.Type == "PURCHASED" && t.Amount > 0)
                    .SumAsync(t => (decimal)t.Amount, cancellationToken);

                monthlyRevenue.Add(new MonthlyRevenueDto
                {
                    Month = monthStart.ToString("MM/yyyy"),
                    CoinAmount = monthCoin,
                    PremiumAmount = 0m,
                    Total = monthCoin
                });
            }

            return new RevenueStatsDto
            {
                TotalCoinTransactions = totalCoinTransactions,
                TotalCoinTransactionsCount = totalCoinTransactionsCount,
                TotalPremiumOrders = totalPremiumOrders,
                TotalPremiumOrdersCount = totalPremiumOrdersCount,
                TotalRevenue = totalRevenue,
                DailyRevenue = dailyRevenue,
                MonthlyRevenue = monthlyRevenue
            };
        }
    }
}
