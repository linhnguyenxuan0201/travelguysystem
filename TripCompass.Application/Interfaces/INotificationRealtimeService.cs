using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces
{
    public interface INotificationRealtimeService
    {
        Task PushUnreadCountAsync(long userId, int unreadCount);
        Task PushNotificationAsync(long userId, Notification notification, int unreadCount);
    }
}

