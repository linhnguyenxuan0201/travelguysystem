using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _db;

        public ChatRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<ChatThread?> GetThreadByBookingIdAsync(long bookingId)
        {
            return _db.ChatThreads.FirstOrDefaultAsync(t => t.BookingId == bookingId);
        }

        public Task<ChatThread?> GetThreadByIdAsync(long chatThreadId)
        {
            return _db.ChatThreads.FirstOrDefaultAsync(t => t.ChatThreadId == chatThreadId);
        }

        public async Task<List<ChatThread>> GetThreadsForUserAsync(long userId, bool isPartner, int page = 1, int pageSize = 20)
        {
            var query = _db.ChatThreads.AsNoTracking();

            query = isPartner
                ? query.Where(t => t.PartnerUserId == userId)
                : query.Where(t => t.CustomerUserId == userId);

            var threads = await query
                .OrderByDescending(t => t.LastMessageAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy tin nhắn cuối cùng cho mỗi thread
            var threadIds = threads.Select(t => t.ChatThreadId).ToList();
            if (threadIds.Count == 0)
            {
                return threads;
            }

            // Lấy tin nhắn cuối cùng - query từng thread riêng để tránh lỗi EF Core
            var lastMessages = new Dictionary<long, (string Content, DateTime CreatedAt)>();
            
            foreach (var threadId in threadIds)
            {
                var lastMessage = await _db.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.ChatThreadId == threadId)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastMessage != null)
                {
                    lastMessages[threadId] = (lastMessage.Content, lastMessage.CreatedAt);
                }
            }

            // Gán LastMessage vào thread (không phải DB column, chỉ là property)
            foreach (var thread in threads)
            {
                if (lastMessages.TryGetValue(thread.ChatThreadId, out var msg))
                {
                    thread.LastMessage = msg.Content;
                    thread.LastMessageAt = msg.CreatedAt;
                }
            }

            return threads;
        }

        public async Task<ChatThread> CreateThreadIfNotExistsAsync(long bookingId, long customerUserId, long partnerUserId)
        {
            var existing = await _db.ChatThreads.FirstOrDefaultAsync(t => t.BookingId == bookingId);
            if (existing != null) return existing;

            var now = DateTime.UtcNow;
            var thread = new ChatThread
            {
                BookingId = bookingId,
                CustomerUserId = customerUserId,
                PartnerUserId = partnerUserId,
                CreatedAt = now,
                LastMessageAt = now,
                CustomerUnreadCount = 0,
                PartnerUnreadCount = 0
            };

            _db.ChatThreads.Add(thread);
            await _db.SaveChangesAsync();
            return thread;
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(long chatThreadId, int page = 1, int pageSize = 50)
        {
            // oldest -> newest for UI
            return await _db.ChatMessages
                .AsNoTracking()
                .Where(m => m.ChatThreadId == chatThreadId)
                .OrderBy(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<ChatMessage> AddMessageAsync(long chatThreadId, long senderUserId, long receiverUserId, string content, string? imageUrl = null, string messageType = "Text")
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Content or ImageUrl is required");

            content = (content ?? "").Trim();
            if (content.Length > 2000) content = content[..2000];
            if (string.IsNullOrWhiteSpace(content) && !string.IsNullOrWhiteSpace(imageUrl))
                content = "📷 Đã gửi ảnh";

            var now = DateTime.UtcNow;

            var message = new ChatMessage
            {
                ChatThreadId = chatThreadId,
                SenderUserId = senderUserId,
                ReceiverUserId = receiverUserId,
                Content = content,
                ImageUrl = imageUrl,
                MessageType = messageType,
                IsRead = false,
                CreatedAt = now
            };

            _db.ChatMessages.Add(message);

            var thread = await _db.ChatThreads.FirstAsync(t => t.ChatThreadId == chatThreadId);
            thread.LastMessageAt = now;

            // increment unread counter for receiver side
            if (receiverUserId == thread.CustomerUserId)
                thread.CustomerUnreadCount += 1;
            else if (receiverUserId == thread.PartnerUserId)
                thread.PartnerUnreadCount += 1;

            await _db.SaveChangesAsync();
            return message;
        }

        public async Task MarkThreadAsReadAsync(long chatThreadId, long userId, bool isPartner)
        {
            var thread = await _db.ChatThreads.FirstOrDefaultAsync(t => t.ChatThreadId == chatThreadId);
            if (thread == null) return;

            // mark messages as read for this user
            var msgs = await _db.ChatMessages
                .Where(m => m.ChatThreadId == chatThreadId && m.ReceiverUserId == userId && !m.IsRead)
                .ToListAsync();

            foreach (var m in msgs) m.IsRead = true;

            if (isPartner)
                thread.PartnerUnreadCount = 0;
            else
                thread.CustomerUnreadCount = 0;

            await _db.SaveChangesAsync();
        }

        public async Task<int> GetUnreadThreadCountAsync(long userId, bool isPartner)
        {
            return isPartner
                ? await _db.ChatThreads.CountAsync(t => t.PartnerUserId == userId && t.PartnerUnreadCount > 0)
                : await _db.ChatThreads.CountAsync(t => t.CustomerUserId == userId && t.CustomerUnreadCount > 0);
        }
    }
}

