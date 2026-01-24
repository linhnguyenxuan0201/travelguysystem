namespace TripCompass.Domain.Entities
{
    public class Notification
    {
        public long NotificationId { get; set; }
        public long UserId { get; set; }
        public string Type { get; set; } = null!; // "NEW_ORDER", "ORDER_APPROVED", "ORDER_REJECTED", "NEW_COMMENT", "NEW_FOLLOW", "COMMISSION_PAID", "WITHDRAW_COMPLETED"
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Link { get; set; } // URL để điều hướng khi click
        public long? ReferenceId { get; set; } // ID của đối tượng liên quan (BookingId, CommentId, etc.)
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User User { get; set; } = null!;
    }
}
