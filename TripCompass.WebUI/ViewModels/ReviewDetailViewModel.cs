using TripCompass.Domain.Enums;

namespace TripCompass.WebUI.ViewModels
{
    public class ReviewDetailViewModel
    {
        // Post Info
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Location { get; set; }
        public PostStatus Status { get; set; }
        public int Rating { get; set; }
        public decimal? Price { get; set; }
        
        // Stats
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int CommentCount { get; set; }
        
        // Dates
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        
        // Author Info
        public long AuthorId { get; set; }
        public string AuthorName { get; set; } = null!;
        public string? AuthorAvatar { get; set; }
        public int AuthorReputationScore { get; set; }
        public int AuthorReputationLevel { get; set; }
        public int AuthorPostCount { get; set; }
        public int AuthorFollowerCount { get; set; }
        public string? AuthorBio { get; set; }
        public bool IsFollowing { get; set; } // Current user đã follow author chưa
        
        // Categories
        public List<string> Categories { get; set; } = new();
        
        // Images
        public List<string> Images { get; set; } = new();
        public string? CoverImage { get; set; }
        
        // Contact Info (if available)
        public string? OpeningHours { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ParkingInfo { get; set; }
        
        // Comments
        public List<CommentViewModel> Comments { get; set; } = new();
        
        // Similar Posts
        public List<SimilarPostViewModel> SimilarPosts { get; set; } = new();
    }
    
    public class CommentViewModel
    {
        public long CommentId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? UserAvatar { get; set; }
        public string Content { get; set; } = null!;
        public int Rating { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public bool UserLiked { get; set; }
        public bool UserDisliked { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = null!;
        public long? ParentCommentId { get; set; }
        public List<CommentViewModel> Replies { get; set; } = new();
        public int ReplyCount { get; set; }
    }
    
    public class SimilarPostViewModel
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public int Rating { get; set; }
        public decimal? Price { get; set; }
    }
}
