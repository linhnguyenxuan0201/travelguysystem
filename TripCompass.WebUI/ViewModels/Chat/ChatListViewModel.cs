namespace TripCompass.WebUI.ViewModels.Chat
{
    public class ChatListViewModel
    {
        public List<ChatThreadItem> Threads { get; set; } = new();
        public int UnreadCount { get; set; }
    }

    public class ChatThreadItem
    {
        public long ChatThreadId { get; set; }
        public long BookingId { get; set; }
        public string BookingTitle { get; set; } = null!;
        public long OtherUserId { get; set; }
        public string OtherUserName { get; set; } = null!;
        public string? OtherUserAvatar { get; set; }
        public string LastMessage { get; set; } = null!;
        public DateTime? LastMessageAt { get; set; }
        public bool IsUnread { get; set; }
        public int UnreadCount { get; set; }
    }
}
