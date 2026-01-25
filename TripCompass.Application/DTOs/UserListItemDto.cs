using System;

namespace TripCompass.Application.DTOs
{
    public class UserListItemDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ReputationScore { get; set; }
        public bool IsBanned { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Roles { get; set; } = string.Empty; // Comma separated roles
    }
}
