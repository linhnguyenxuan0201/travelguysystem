using System;
using Microsoft.AspNetCore.SignalR;
using TripCompass.Application.Interfaces;
using TripCompass.WebUI.Hubs;

namespace TripCompass.WebUI.Services
{
    public class SignalRChatRealtimeService : IChatRealtimeService
    {
        private readonly IHubContext<ChatHub> _hub;

        public SignalRChatRealtimeService(IHubContext<ChatHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyMessageReceivedAsync(long receiverUserId, long chatThreadId, long messageId, string content, string senderName, string? imageUrl = null, string messageType = "Text")
        {
            return _hub.Clients.Group($"user_{receiverUserId}")
                .SendAsync("MessageReceived", chatThreadId, messageId, content, senderName, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"), imageUrl, messageType);
        }

        public Task NotifyTypingAsync(long receiverUserId, long chatThreadId, string senderName, bool isTyping)
        {
            return _hub.Clients.Group($"user_{receiverUserId}")
                .SendAsync("Typing", chatThreadId, senderName, isTyping);
        }

        public Task NotifyChatUnreadCountUpdatedAsync(long userId, int unreadCount)
        {
            return _hub.Clients.Group($"user_{userId}")
                .SendAsync("ChatUnreadCountUpdated", unreadCount);
        }
    }
}

