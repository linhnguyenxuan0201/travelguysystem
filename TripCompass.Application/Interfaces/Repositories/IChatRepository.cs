using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface IChatRepository
    {
        Task<ChatThread?> GetThreadByBookingIdAsync(long bookingId);
        Task<ChatThread?> GetThreadByIdAsync(long chatThreadId);
        Task<List<ChatThread>> GetThreadsForUserAsync(long userId, bool isPartner, int page = 1, int pageSize = 20);

        Task<ChatThread> CreateThreadIfNotExistsAsync(long bookingId, long customerUserId, long partnerUserId);

        Task<List<ChatMessage>> GetMessagesAsync(long chatThreadId, int page = 1, int pageSize = 50);
        Task<ChatMessage> AddMessageAsync(long chatThreadId, long senderUserId, long receiverUserId, string content, string? imageUrl = null, string messageType = "Text");

        Task MarkThreadAsReadAsync(long chatThreadId, long userId, bool isPartner);
        Task<int> GetUnreadThreadCountAsync(long userId, bool isPartner);
    }
}

