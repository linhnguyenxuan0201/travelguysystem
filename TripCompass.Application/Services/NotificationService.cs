using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Services
{
    public class NotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationRealtimeService? _realtime;

        public NotificationService(
            INotificationRepository notificationRepository,
            INotificationRealtimeService? realtime = null)
        {
            _notificationRepository = notificationRepository;
            _realtime = realtime;
        }

        public async Task NotifyNewOrderAsync(long partnerUserId, long bookingId, string customerName, decimal totalAmount)
        {
            var notification = new Notification
            {
                UserId = partnerUserId,
                Type = "NEW_ORDER",
                Title = "Đơn hàng mới",
                Message = $"Bạn có đơn hàng mới từ {customerName} với tổng tiền {totalAmount:N0}₫",
                Link = $"/Partner/Orders",
                ReferenceId = bookingId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(partnerUserId, notification);
        }

        public async Task NotifyOrderApprovedAsync(long customerUserId, long bookingId)
        {
            var notification = new Notification
            {
                UserId = customerUserId,
                Type = "ORDER_APPROVED",
                Title = "Đơn hàng được duyệt",
                Message = $"Đơn hàng #{bookingId} của bạn đã được shop duyệt.",
                Link = $"/Booking/Detail/{bookingId}",
                ReferenceId = bookingId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(customerUserId, notification);
        }

        public async Task NotifyOrderRejectedAsync(long customerUserId, long bookingId, string reason)
        {
            var notification = new Notification
            {
                UserId = customerUserId,
                Type = "ORDER_REJECTED",
                Title = "Đơn hàng bị từ chối",
                Message = $"Đơn hàng #{bookingId} của bạn đã bị shop từ chối. Lý do: {reason}",
                Link = $"/Booking/Detail/{bookingId}",
                ReferenceId = bookingId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(customerUserId, notification);
        }

        public async Task NotifyNewCommentAsync(long postAuthorId, long postId, long commentId, string commenterName)
        {
            var notification = new Notification
            {
                UserId = postAuthorId,
                Type = "NEW_COMMENT",
                Title = "Bình luận mới",
                Message = $"{commenterName} đã bình luận bài viết của bạn.",
                Link = $"/Review/Detail/{postId}#comment-{commentId}",
                ReferenceId = commentId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(postAuthorId, notification);
        }

        public async Task NotifyNewFollowAsync(long followingUserId, string followerName)
        {
            var notification = new Notification
            {
                UserId = followingUserId,
                Type = "NEW_FOLLOW",
                Title = "Người theo dõi mới",
                Message = $"{followerName} đã theo dõi bạn.",
                Link = $"/Account/Profile",
                ReferenceId = null,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(followingUserId, notification);
        }

        public async Task NotifyCommissionPaidAsync(long partnerUserId, long bookingId, decimal commissionAmount)
        {
            var notification = new Notification
            {
                UserId = partnerUserId,
                Type = "COMMISSION_PAID",
                Title = "Hoa hồng đã được thanh toán",
                Message = $"Hoa hồng {commissionAmount:N0}₫ cho đơn hàng #{bookingId} đã được thanh toán.",
                Link = $"/Partner/Commission",
                ReferenceId = bookingId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(partnerUserId, notification);
        }

        public async Task NotifyWithdrawCompletedAsync(long userId, decimal amount)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = "WITHDRAW_COMPLETED",
                Title = "Rút tiền thành công",
                Message = $"Yêu cầu rút tiền {amount:N0}₫ của bạn đã được xử lý.",
                Link = $"/Account/Wallet",
                ReferenceId = null,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(userId, notification);
        }

        public async Task NotifyNewMessageAsync(long receiverUserId, long chatThreadId, long bookingId, string senderName, string messagePreview)
        {
            var notification = new Notification
            {
                UserId = receiverUserId,
                Type = "NEW_MESSAGE",
                Title = "Tin nhắn mới",
                Message = $"{senderName}: {messagePreview}",
                Link = $"/Chat/Chat?bookingId={bookingId}",
                ReferenceId = chatThreadId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(receiverUserId, notification);
        }

        public async Task CreateNotificationAsync(
            long userId,
            string type,
            string title,
            string message,
            string? link = null,
            long? referenceId = null,
            CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Link = link,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await PushRealtimeAsync(userId, notification);
        }

        private async Task PushRealtimeAsync(long userId, Notification notification)
        {
            if (_realtime == null) return;

            try
            {
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);
                await _realtime.PushNotificationAsync(userId, notification, unreadCount);
            }
            catch
            {
                // Ignore realtime push errors
            }
        }
    }
}
