using System;

namespace TripCompass.Domain.Entities
{
    public class PostImage
    {
        public long PostImageId { get; set; }
        public long PostId { get; set; }

        public string ImageUrl { get; set; } = null!;
        public bool IsCover { get; set; }
        public int SortOrder { get; set; }

        // ✅ THÊM DÒNG NÀY
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Post Post { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
    }
}
