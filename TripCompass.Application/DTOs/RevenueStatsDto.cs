namespace TripCompass.Application.DTOs
{
    public class RevenueStatsDto
    {
        public decimal TotalCoinTransactions { get; set; }
        public int TotalCoinTransactionsCount { get; set; }
        public decimal TotalPremiumOrders { get; set; }
        public int TotalPremiumOrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    }

    public class DailyRevenueDto
    {
        public string Date { get; set; } = null!;
        public decimal CoinAmount { get; set; }
        public decimal PremiumAmount { get; set; }
        public decimal Total { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; } = null!;
        public decimal CoinAmount { get; set; }
        public decimal PremiumAmount { get; set; }
        public decimal Total { get; set; }
    }
}
