using Microsoft.AspNetCore.SignalR;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;
using TripCompass.WebUI.Hubs;

namespace TripCompass.WebUI.Services
{
    public class SignalRNotificationRealtimeService : INotificationRealtimeService
    {
        private readonly IHubContext<NotificationHub> _hub;

        public SignalRNotificationRealtimeService(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

        public Task PushUnreadCountAsync(long userId, int unreadCount)
        {
            return _hub.Clients.Group($"user_{userId}")
                .SendAsync("UnreadCountUpdated", unreadCount);
        }

        public Task PushNotificationAsync(long userId, Notification notification, int unreadCount)
        {
            var payload = new
            {
                notificationId = notification.NotificationId,
                type = notification.Type,
                title = notification.Title,
                message = notification.Message,
                link = notification.Link,
                referenceId = notification.ReferenceId,
                createdAt = notification.CreatedAt,
                isRead = notification.IsRead,
                unreadCount
            };

            return _hub.Clients.Group($"user_{userId}")
                .SendAsync("NotificationReceived", payload);
        }
    }
}

