namespace TripCompass.Domain.Entities
{
    public class CommentReaction
    {
        public long ReactionId { get; set; }
        public long CommentId { get; set; }
        public long UserId { get; set; }
        public string ReactionType { get; set; } = null!; // LIKE, DISLIKE
        public DateTime CreatedAt { get; set; }

        // Navigation
        public PostComment Comment { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
