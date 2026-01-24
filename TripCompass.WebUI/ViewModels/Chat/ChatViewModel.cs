namespace TripCompass.WebUI.ViewModels.Chat
{
    public class ChatViewModel
    {
        public long ChatThreadId { get; set; }
        public long BookingId { get; set; }
        public string BookingTitle { get; set; } = null!;
        public long OtherUserId { get; set; }
        public string OtherUserName { get; set; } = null!;
        public string? OtherUserAvatar { get; set; }
        public List<ChatMessageItem> Messages { get; set; } = new();
        public bool IsPartner { get; set; }
    }

    public class ChatMessageItem
    {
        public long MessageId { get; set; }
        public long SenderUserId { get; set; }
        public string SenderName { get; set; } = null!;
        public string? SenderAvatar { get; set; }
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string MessageType { get; set; } = "Text";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsMine { get; set; }
    }
}
