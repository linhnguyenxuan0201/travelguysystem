namespace TripCompass.Application.Interfaces
{
    public interface IChatRealtimeService
    {
        Task NotifyMessageReceivedAsync(long receiverUserId, long chatThreadId, long messageId, string content, string senderName, string? imageUrl = null, string messageType = "Text");
        Task NotifyChatUnreadCountUpdatedAsync(long userId, int unreadCount);
        Task NotifyTypingAsync(long receiverUserId, long chatThreadId, string senderName, bool isTyping);
    }
}
