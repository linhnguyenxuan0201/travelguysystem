namespace TripCompass.Application.DTOs
{
    public class CommentListItemDto
    {
        public long CommentId { get; set; }
        public long PostId { get; set; }
        public string PostTitle { get; set; } = null!;
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public long? ParentCommentId { get; set; }
        public string Content { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReactionCount { get; set; }
    }
}
