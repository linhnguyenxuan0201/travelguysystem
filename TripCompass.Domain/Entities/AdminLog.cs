using System;

namespace TripCompass.Domain.Entities
{
    public class AdminLog
    {
        public long LogId { get; set; }
        public long AdminId { get; set; }
        public string ActionType { get; set; } = null!;
        public string TargetTable { get; set; } = null!;
        public long TargetId { get; set; }
        public string? Note { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }

        public User Admin { get; set; } = null!;
    }
}
