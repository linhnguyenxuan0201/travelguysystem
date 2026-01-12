using System;

namespace TripCompass.Domain.Entities
{
    public class Report
    {
        public long ReportId { get; set; }
        public long ReporterId { get; set; }
        public long TargetId { get; set; }
        public string TargetType { get; set; } = null!; // POST | COMMENT | USER
        public string Reason { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; } // 0 Pending | 1 Resolved | 2 Rejected
        public long? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public User Reporter { get; set; } = null!;
        public User? Resolver { get; set; }
    }
}
