using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Application.Services;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;
using TripCompass.WebUI.ViewModels.Chat;

namespace TripCompass.WebUI.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IChatRepository _chatRepository;
        private readonly NotificationService _notificationService;
        private readonly TripCompass.Application.Interfaces.IChatRealtimeService _chatRealtime;

        public ChatController(
            AppDbContext context,
            ICurrentUserService currentUser,
            IChatRepository chatRepository,
            NotificationService notificationService,
            TripCompass.Application.Interfaces.IChatRealtimeService chatRealtime)
        {
            _context = context;
            _currentUser = currentUser;
            _chatRepository = chatRepository;
            _notificationService = notificationService;
            _chatRealtime = chatRealtime;
        }

        // Danh sách cuộc chat
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            var isPartner = User.IsInRole("Partner");

            var threads = await _chatRepository.GetThreadsForUserAsync(userId, isPartner, page: 1, pageSize: 50);
            var unreadCount = await _chatRepository.GetUnreadThreadCountAsync(userId, isPartner);

            // Lấy thông tin booking và user
            var bookingIds = threads.Select(t => t.BookingId).Distinct().ToList();
            var bookings = await _context.PostBookings
                .AsNoTracking()
                .Include(b => b.Post)
                .Where(b => bookingIds.Contains(b.BookingId))
                .ToDictionaryAsync(b => b.BookingId);

            var otherUserIds = threads.Select(t => isPartner ? t.CustomerUserId : t.PartnerUserId).Distinct().ToList();
            var otherUsers = await _context.Users
                .AsNoTracking()
                .Where(u => otherUserIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId);

            var otherUserAvatars = await _context.UserAvatars
                .AsNoTracking()
                .Where(a => otherUserIds.Contains(a.UserId) && a.IsActive)
                .ToDictionaryAsync(a => a.UserId, a => a.AvatarUrl);

            var threadItems = threads.Select(t =>
            {
                var booking = bookings.GetValueOrDefault(t.BookingId);
                var otherUserId = isPartner ? t.CustomerUserId : t.PartnerUserId;
                var otherUser = otherUsers.GetValueOrDefault(otherUserId);

                return new ChatThreadItem
                {
                    ChatThreadId = t.ChatThreadId,
                    BookingId = t.BookingId,
                    BookingTitle = booking?.Post?.Title ?? $"Booking #{t.BookingId}",
                    OtherUserId = otherUserId,
                    OtherUserName = otherUser?.UserName ?? "Unknown",
                    OtherUserAvatar = otherUserAvatars.GetValueOrDefault(otherUserId),
                    LastMessage = t.LastMessage ?? "",
                    LastMessageAt = t.LastMessageAt,
                    IsUnread = isPartner ? t.PartnerUnreadCount > 0 : t.CustomerUnreadCount > 0,
                    UnreadCount = isPartner ? t.PartnerUnreadCount : t.CustomerUnreadCount
                };
            }).ToList();

            var vm = new ChatListViewModel
            {
                Threads = threadItems,
                UnreadCount = unreadCount
            };

            return View(vm);
        }

        // Trang chat theo BookingId
        [HttpGet]
        public async Task<IActionResult> Chat(long bookingId)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return RedirectToAction("Login", "Account");

            var booking = await _context.PostBookings
                .AsNoTracking()
                .Include(b => b.Post)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            var isPartner = User.IsInRole("Partner");
            var isOwner = isPartner ? booking.PartnerUserId == userId : booking.CustomerUserId == userId;

            if (!isOwner)
            {
                TempData["Error"] = "Bạn không có quyền truy cập cuộc chat này.";
                return RedirectToAction("Index");
            }

            // Tạo thread nếu chưa có
            var thread = await _chatRepository.CreateThreadIfNotExistsAsync(
                bookingId,
                booking.CustomerUserId,
                booking.PartnerUserId
            );

            // Đánh dấu đã đọc
            await _chatRepository.MarkThreadAsReadAsync(thread.ChatThreadId, userId, isPartner);

            // Lấy tin nhắn
            var messages = await _chatRepository.GetMessagesAsync(thread.ChatThreadId, page: 1, pageSize: 100);

            // Lấy thông tin user
            var otherUserId = isPartner ? booking.CustomerUserId : booking.PartnerUserId;
            var otherUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == otherUserId);

            var otherUserAvatar = await _context.UserAvatars
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == otherUserId && a.IsActive);

            var senderUserIds = messages.Select(m => m.SenderUserId).Distinct().ToList();
            var senderUsers = await _context.Users
                .AsNoTracking()
                .Where(u => senderUserIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId);

            var senderAvatars = await _context.UserAvatars
                .AsNoTracking()
                .Where(a => senderUserIds.Contains(a.UserId) && a.IsActive)
                .ToDictionaryAsync(a => a.UserId, a => a.AvatarUrl);

            var messageItems = messages.Select(m => new ChatMessageItem
            {
                MessageId = m.ChatMessageId,
                SenderUserId = m.SenderUserId,
                SenderName = senderUsers.GetValueOrDefault(m.SenderUserId)?.UserName ?? "Unknown",
                SenderAvatar = senderAvatars.GetValueOrDefault(m.SenderUserId),
                Content = m.Content,
                ImageUrl = m.ImageUrl,
                MessageType = m.MessageType,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead,
                IsMine = m.SenderUserId == userId
            }).ToList();

            var vm = new ChatViewModel
            {
                ChatThreadId = thread.ChatThreadId,
                BookingId = bookingId,
                BookingTitle = booking.Post?.Title ?? $"Booking #{bookingId}",
                OtherUserId = otherUserId,
                OtherUserName = otherUser?.UserName ?? "Unknown",
                OtherUserAvatar = otherUserAvatar?.AvatarUrl,
                Messages = messageItems,
                IsPartner = isPartner
            };

            return View(vm);
        }

        // Gửi tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(long chatThreadId, string? content, IFormFile? image)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized();

            var thread = await _chatRepository.GetThreadByIdAsync(chatThreadId);
            if (thread == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy cuộc chat." });
            }

            var isPartner = User.IsInRole("Partner");
            var isOwner = isPartner ? thread.PartnerUserId == userId : thread.CustomerUserId == userId;

            if (!isOwner)
            {
                return Forbid();
            }

            var receiverUserId = isPartner ? thread.CustomerUserId : thread.PartnerUserId;

            string? imageUrl = null;
            string messageType = "Text";

            // Upload ảnh nếu có
            if (image != null && image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, png, gif, webp)." });
                }

                if (image.Length > 5 * 1024 * 1024) // 5MB
                {
                    return BadRequest(new { success = false, message = "Kích thước ảnh không được vượt quá 5MB." });
                }

                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/chat");
                Directory.CreateDirectory(uploadDir);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imageUrl = $"/images/chat/{fileName}";
                messageType = "Image";
            }

            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest(new { success = false, message = "Nội dung tin nhắn hoặc ảnh không được để trống." });
            }

            var message = await _chatRepository.AddMessageAsync(
                chatThreadId,
                userId,
                receiverUserId,
                content?.Trim() ?? "",
                imageUrl,
                messageType
            );

            // Lấy thông tin sender và booking
            var sender = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            var booking = await _context.PostBookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.BookingId == thread.BookingId);

            // Gửi thông báo tin nhắn mới
            try
            {
                var messagePreview = message.Content.Length > 50 
                    ? message.Content.Substring(0, 50) + "..." 
                    : message.Content;

                await _notificationService.NotifyNewMessageAsync(
                    receiverUserId,
                    chatThreadId,
                    thread.BookingId,
                    sender?.UserName ?? "Người dùng",
                    messagePreview
                );
            }
            catch
            {
                // Ignore notification errors
            }

            // Gửi realtime qua SignalR
            try
            {
                await _chatRealtime.NotifyMessageReceivedAsync(
                    receiverUserId,
                    chatThreadId,
                    message.ChatMessageId,
                    message.Content,
                    sender?.UserName ?? "Người dùng",
                    message.ImageUrl,
                    message.MessageType
                );

                // Cập nhật unread count
                var unreadCount = await _chatRepository.GetUnreadThreadCountAsync(receiverUserId, !isPartner);
                await _chatRealtime.NotifyChatUnreadCountUpdatedAsync(receiverUserId, unreadCount);
            }
            catch
            {
                // Ignore realtime errors
            }

            return Ok(new
            {
                success = true,
                message = new
                {
                    messageId = message.ChatMessageId,
                    senderUserId = message.SenderUserId,
                    content = message.Content,
                    imageUrl = message.ImageUrl,
                    messageType = message.MessageType,
                    sentAt = message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss")
                }
            });
        }

        // API: Lấy số lượng chat chưa đọc
        [HttpGet("api/chat/unread-count")]
        public async Task<IActionResult> GetUnreadChatCount()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized(new { count = 0 });

            var isPartner = User.IsInRole("Partner");
            var unreadCount = await _chatRepository.GetUnreadThreadCountAsync(userId, isPartner);

            return Ok(new { count = unreadCount });
        }

        // API: Lấy danh sách chat threads cho dropdown
        [HttpGet("api/chat/threads")]
        public async Task<IActionResult> GetChatThreads(int page = 1, int pageSize = 10)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized();

            var isPartner = User.IsInRole("Partner");

            var threads = await _chatRepository.GetThreadsForUserAsync(userId, isPartner, page, pageSize);

            // Lấy thông tin booking và user
            var bookingIds = threads.Select(t => t.BookingId).Distinct().ToList();
            var bookings = await _context.PostBookings
                .AsNoTracking()
                .Include(b => b.Post)
                .Where(b => bookingIds.Contains(b.BookingId))
                .ToDictionaryAsync(b => b.BookingId);

            var otherUserIds = threads.Select(t => isPartner ? t.CustomerUserId : t.PartnerUserId).Distinct().ToList();
            var otherUsers = await _context.Users
                .AsNoTracking()
                .Where(u => otherUserIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId);

            var otherUserAvatars = await _context.UserAvatars
                .AsNoTracking()
                .Where(a => otherUserIds.Contains(a.UserId) && a.IsActive)
                .ToDictionaryAsync(a => a.UserId, a => a.AvatarUrl);

            var threadItems = threads.Select(t =>
            {
                var booking = bookings.GetValueOrDefault(t.BookingId);
                var otherUserId = isPartner ? t.CustomerUserId : t.PartnerUserId;
                var otherUser = otherUsers.GetValueOrDefault(otherUserId);
                var otherUserAvatar = otherUserAvatars.GetValueOrDefault(otherUserId);

                return new
                {
                    chatThreadId = t.ChatThreadId,
                    bookingId = t.BookingId,
                    bookingTitle = booking?.Post?.Title ?? $"Booking #{t.BookingId}",
                    otherUserId = otherUserId,
                    otherUserName = otherUser?.UserName ?? "Unknown",
                    otherUserAvatar = otherUserAvatar,
                    lastMessage = t.LastMessage ?? "",
                    lastMessageAt = t.LastMessageAt,
                    isUnread = isPartner ? t.PartnerUnreadCount > 0 : t.CustomerUnreadCount > 0,
                    unreadCount = isPartner ? t.PartnerUnreadCount : t.CustomerUnreadCount
                };
            }).ToList();

            return Ok(threadItems);
        }

        // API: Lấy messages cho modal
        [HttpGet("GetMessages")]
        public async Task<IActionResult> GetMessages(long chatThreadId)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized();

            var thread = await _chatRepository.GetThreadByIdAsync(chatThreadId);
            if (thread == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy cuộc chat." });
            }

            var isPartner = User.IsInRole("Partner");
            var isOwner = isPartner ? thread.PartnerUserId == userId : thread.CustomerUserId == userId;

            if (!isOwner)
            {
                return Forbid();
            }

            var messages = await _chatRepository.GetMessagesAsync(chatThreadId, page: 1, pageSize: 50);

            // Lấy thông tin sender
            var senderUserIds = messages.Select(m => m.SenderUserId).Distinct().ToList();
            var senders = await _context.Users
                .AsNoTracking()
                .Where(u => senderUserIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId);

            var messageItems = messages.Select(m => new
            {
                messageId = m.ChatMessageId,
                senderUserId = m.SenderUserId,
                senderName = senders.GetValueOrDefault(m.SenderUserId)?.UserName ?? "Unknown",
                content = m.Content,
                imageUrl = m.ImageUrl,
                messageType = m.MessageType,
                createdAt = m.CreatedAt,
                isRead = m.IsRead
            }).ToList();

            // Mark as read
            await _chatRepository.MarkThreadAsReadAsync(chatThreadId, userId, isPartner);

            return Ok(new { success = true, messages = messageItems });
        }
    }
}
