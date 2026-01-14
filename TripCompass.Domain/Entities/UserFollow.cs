namespace TripCompass.Domain.Entities
{
    public class UserFollow
    {
        public long FollowId { get; set; }
        public long FollowerId { get; set; } // Người theo dõi
        public long FollowingId { get; set; } // Người được theo dõi
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User Follower { get; set; } = null!;
        public User Following { get; set; } = null!;
    }
}
