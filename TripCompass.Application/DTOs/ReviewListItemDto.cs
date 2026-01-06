using System;
using System.Collections.Generic;
using System.Text;
using TripCompass.Domain.Entities;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.DTOs
{
    public class ReviewListItemDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public List<string> Categories { get; set; } = new();
        public int Rating { get; set; }
        public string? Location { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        // ✅ THÊM CÁI NÀY
        public string ThumbnailUrl { get; set; }
     public PostStatus Status { get; set; }
    }

}
