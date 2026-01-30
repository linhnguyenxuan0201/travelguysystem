namespace TripCompass.Domain.Entities
{
    public class Notification
    {
        public long NotificationId { get; set; }
        public long UserId { get; set; } // User nhận thông báo
        public string Type { get; set; } = null!; // NEW_COMMENT, POST_APPROVED, POST_REJECTED, COIN_EARNED
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Link { get; set; } // Link đến bài viết/comment
        public long? ReferenceId { get; set; } // ID của Post/Comment/Transaction liên quan
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}
