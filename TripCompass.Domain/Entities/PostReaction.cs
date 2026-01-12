using System;

namespace TripCompass.Domain.Entities
{
    public class PostReaction
    {
        public long ReactionId { get; set; }
        public long PostId { get; set; }
        public long UserId { get; set; }
        public string ReactionType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Post Post { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
