namespace TripCompass.Application.DTOs
{
    public class ReportListItemDto
    {
        public long ReportId { get; set; }
        public long ReporterId { get; set; }
        public string ReporterName { get; set; } = null!;
        public string ReporterEmail { get; set; } = null!;
        public long TargetId { get; set; }
        public string TargetType { get; set; } = null!; // POST | COMMENT | USER
        public string Reason { get; set; } = null!;
        public string? Description { get; set; }
        public int Status { get; set; } // 0 Pending | 1 Resolved | 2 Rejected
        public string StatusDisplay { get; set; } = null!;
        public long? ResolvedBy { get; set; }
        public string? ResolverName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Target details (if available)
        public string? TargetTitle { get; set; } // For POST
        public string? TargetUserName { get; set; } // For USER
    }
}
