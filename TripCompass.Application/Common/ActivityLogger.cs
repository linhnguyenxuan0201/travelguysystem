using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Common
{
    public static class ActivityLogger
    {
        /// <summary>
        /// Log activity for any user (Admin, User, Moderator, etc.)
        /// </summary>
        public static async Task LogActivityAsync(
            IApplicationDbContext context,
            long userId,
            string actionType,
            string targetTable,
            long targetId,
            string? note = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var adminLog = new AdminLog
            {
                AdminId = userId, // Reuse AdminId field for all users
                ActionType = actionType,
                TargetTable = targetTable,
                TargetId = targetId,
                Note = note,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            context.AdminLogs.Add(adminLog);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
