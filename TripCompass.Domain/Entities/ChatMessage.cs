using System;

namespace TripCompass.Domain.Entities
{
    public class ChatMessage
    {
        public long ChatMessageId { get; set; }
        public long ChatThreadId { get; set; }

        public long SenderUserId { get; set; }
        public long ReceiverUserId { get; set; }

        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; } // URL của ảnh nếu có
        public string MessageType { get; set; } = "Text"; // Text, Image, File
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Alias for CreatedAt (for UI compatibility)
        public DateTime SentAt => CreatedAt;
    }
}

