using System;

namespace TripCompass.Domain.Entities
{
    public class CoinTransaction
    {
        public long TransactionId { get; set; }
        public long UserId { get; set; }
        public int Amount { get; set; }
        public string Type { get; set; } = null!;
        public long? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
    }
}
