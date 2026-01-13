using System.Collections.Generic;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Common
{
    /// <summary>
    /// Service để validate workflow transitions cho Post Status
    /// </summary>
    public static class PostStatusWorkflow
    {
        /// <summary>
        /// Kiểm tra xem transition từ currentStatus sang newStatus có hợp lệ không
        /// </summary>
        public static bool IsValidTransition(PostStatus currentStatus, PostStatus newStatus)
        {
            // Deleted có thể từ bất kỳ status nào (xử lý riêng bằng IsDeleted flag)
            // Ở đây chỉ validate status transitions, không validate Deleted
            
            return currentStatus switch
            {
                PostStatus.Draft => newStatus == PostStatus.Pending, // Draft chỉ có thể → PendingApproval
                
                PostStatus.Pending => newStatus == PostStatus.Published || 
                                     newStatus == PostStatus.Rejected, // PendingApproval → Published hoặc Rejected
                
                PostStatus.Published => newStatus == PostStatus.Archived, // Published → Archived
                
                PostStatus.Rejected => newStatus == PostStatus.Draft || 
                                      newStatus == PostStatus.Pending, // Rejected có thể quay lại Draft hoặc Pending
                
                PostStatus.Archived => newStatus == PostStatus.Published, // Archived → Published
                
                _ => false
            };
        }

        /// <summary>
        /// Lấy danh sách các status hợp lệ có thể chuyển từ currentStatus
        /// </summary>
        public static List<PostStatus> GetValidNextStatuses(PostStatus currentStatus)
        {
            return currentStatus switch
            {
                PostStatus.Draft => new List<PostStatus> { PostStatus.Pending },
                
                PostStatus.Pending => new List<PostStatus> { PostStatus.Published, PostStatus.Rejected },
                
                PostStatus.Published => new List<PostStatus> { PostStatus.Archived },
                
                PostStatus.Rejected => new List<PostStatus> { PostStatus.Draft, PostStatus.Pending },
                
                PostStatus.Archived => new List<PostStatus> { PostStatus.Published },
                
                _ => new List<PostStatus>()
            };
        }

        /// <summary>
        /// Kiểm tra xem có thể publish trực tiếp từ Draft không (không được phép nếu có moderation)
        /// </summary>
        public static bool CanPublishFromDraft(bool requiresModeration = true)
        {
            return !requiresModeration;
        }
    }
}
