using System;
using System.Collections.Generic;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Posts.GetPostById
{
    public class PostDetailDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Location { get; set; }
        public PostStatus Status { get; set; }
        
        public long UserId { get; set; }
        public string AuthorName { get; set; } = null!;
        public string? AuthorAvatar { get; set; }
        public string? AuthorEmail { get; set; }

        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        
        // SEO
        public string? Slug { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? CanonicalUrl { get; set; }
        public bool IsIndexable { get; set; }

        // Flags
        public bool IsFeatured { get; set; }
        public bool IsTrending { get; set; }
        public bool IsPinned { get; set; }

        // Moderation
        public string? ModerationNote { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Images { get; set; } = new List<string>();
    }
}
