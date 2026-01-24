using System;

namespace TripCompass.Domain.Entities
{
    public class ChatThread
    {
        public long ChatThreadId { get; set; }
        public long BookingId { get; set; }

        public long CustomerUserId { get; set; }
        public long PartnerUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public int CustomerUnreadCount { get; set; } = 0;
        public int PartnerUnreadCount { get; set; } = 0;

        // Navigation properties (không map vào DB, chỉ để query)
        public string? LastMessage { get; set; }
    }
}

