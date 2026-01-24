using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(long notificationId);
        Task<List<Notification>> GetByUserIdAsync(long userId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(long userId);
        Task AddAsync(Notification notification);
        Task MarkAsReadAsync(long notificationId);
        Task MarkAllAsReadAsync(long userId);
    }
}
