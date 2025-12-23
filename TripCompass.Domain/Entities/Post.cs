using System;
using System.Collections.Generic;
using TripCompass.Domain.Enums;

namespace TripCompass.Domain.Entities
{
    public class Post
    {
        public long PostId { get; set; }

        public long UserId { get; set; }

        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;

        public string? Location { get; set; }

        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }

        public int ReputationImpact { get; set; }

        public bool IsPartner { get; set; }

        public DateTime CreatedAt { get; set; }

        // =========================
        // 🔥 SOFT DELETE
        // =========================
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // =========================
        // NAVIGATION
        // =========================
        public ICollection<PostCategory> PostCategories { get; set; }
            = new List<PostCategory>();

        public ICollection<PostImage> PostImages { get; set; }
            = new List<PostImage>();
        public PostStatus Status { get; set; } = PostStatus.Pending;
    }
}
