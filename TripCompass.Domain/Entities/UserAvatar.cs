using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Entities
{
    public class UserAvatar
    {
        public long UserAvatarId { get; set; }

        // ⚠️ BẮT BUỘC CÓ
        public long UserId { get; set; }

        public string AvatarUrl { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // navigation
        public User User { get; set; } = null!;
    }


}
