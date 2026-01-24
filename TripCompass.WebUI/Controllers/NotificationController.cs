using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces.Repositories;

namespace TripCompass.WebUI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ICurrentUserService _currentUser;

        public NotificationController(
            INotificationRepository notificationRepository,
            ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _currentUser = currentUser;
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized();

            var count = await _notificationRepository.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(int page = 1, int pageSize = 20)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized();

            var notifications = await _notificationRepository.GetByUserIdAsync(userId, page, pageSize);
            return Ok(notifications);
        }

        [HttpPost("{notificationId}/mark-read")]
        public async Task<IActionResult> MarkAsRead(long notificationId)
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized();

            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return NotFound();
            }

            await _notificationRepository.MarkAsReadAsync(notificationId);
            return Ok(new { success = true });
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _currentUser.UserId;
            if (userId <= 0) return Unauthorized();

            await _notificationRepository.MarkAllAsReadAsync(userId);
            return Ok(new { success = true });
        }
    }
}
