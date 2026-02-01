namespace TripCompass.Application.DTOs
{
    public class CoinTransactionListItemDto
    {
        public long TransactionId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public int Amount { get; set; }
        public string Type { get; set; } = null!;
        public string TypeDisplay { get; set; } = null!;
        public long? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
