using System;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Posts.GetPosts
{
    public class PostDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string? AuthorAvatar { get; set; }
        public string CategoryName { get; set; } = null!;
        public PostStatus Status { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        
        // Flags
        public bool IsFeatured { get; set; }
        public bool IsTrending { get; set; }
    }
}
