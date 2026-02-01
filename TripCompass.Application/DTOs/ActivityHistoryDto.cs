namespace TripCompass.Application.DTOs
{
    public class ActivityHistoryDto
    {
        public long LogId { get; set; }
        public long AdminId { get; set; } // UserId of the person who performed the action
        public string AdminName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public string UserRole { get; set; } = null!; // Role of the person who performed the action
        public string ActionType { get; set; } = null!;
        public string TargetTable { get; set; } = null!;
        public long TargetId { get; set; }
        public string? Note { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Helper properties for display
        public string ActionDisplayName => ActivityHistoryHelper.GetActionDisplayName(ActionType);
        public string ActionIcon => ActivityHistoryHelper.GetActionIcon(ActionType);
        public string ActionColor => ActivityHistoryHelper.GetActionColor(ActionType);
    }

    public static class ActivityHistoryHelper
    {
        public static string GetActionDisplayName(string actionType)
        {
            return actionType switch
            {
                "BAN_USER" => "Khóa người dùng",
                "UNBAN_USER" => "Mở khóa người dùng",
                "CHANGE_USER_ROLE" => "Thay đổi quyền người dùng",
                "CHANGE_POST_STATUS" => "Thay đổi trạng thái bài viết",
                "UPDATE_POST" => "Cập nhật bài viết",
                "DELETE_POST" => "Xóa bài viết",
                "RESTORE_POST" => "Khôi phục bài viết",
                "APPROVE_POST" => "Duyệt bài viết",
                "REJECT_POST" => "Từ chối bài viết",
                "ARCHIVE_POST" => "Lưu trữ bài viết",
                "DELETE_COMMENT" => "Xóa bình luận",
                "RESOLVE_REPORT" => "Xử lý báo cáo",
                "REJECT_REPORT" => "Từ chối báo cáo",
                "APPROVE_PARTNER" => "Duyệt đối tác",
                "REJECT_PARTNER" => "Từ chối đối tác",
                "CREATE_CATEGORY" => "Tạo danh mục",
                "UPDATE_CATEGORY" => "Cập nhật danh mục",
                "DELETE_CATEGORY" => "Xóa danh mục",
                "APPROVE_AD_PACKAGE" => "Duyệt gói quảng cáo",
                "UPDATE_SECURITY_SETTINGS" => "Cập nhật cài đặt bảo mật",
                // User actions
                "CREATE_POST" => "Tạo bài viết",
                "CREATE_COMMENT" => "Tạo bình luận",
                "LIKE_COMMENT" => "Thích bình luận",
                "DISLIKE_COMMENT" => "Không thích bình luận",
                "FOLLOW_USER" => "Theo dõi người dùng",
                "UNFOLLOW_USER" => "Bỏ theo dõi người dùng",
                "DELETE_OWN_POST" => "Xóa bài viết của mình",
                _ => actionType
            };
        }

        public static string GetActionIcon(string actionType)
        {
            return actionType switch
            {
                "BAN_USER" => "fa-ban",
                "UNBAN_USER" => "fa-unlock",
                "CHANGE_USER_ROLE" => "fa-user-cog",
                "CHANGE_POST_STATUS" => "fa-exchange-alt",
                "UPDATE_POST" => "fa-edit",
                "DELETE_POST" => "fa-trash",
                "RESTORE_POST" => "fa-undo",
                "APPROVE_POST" => "fa-check-circle",
                "REJECT_POST" => "fa-times-circle",
                "ARCHIVE_POST" => "fa-archive",
                "DELETE_COMMENT" => "fa-comment-slash",
                "RESOLVE_REPORT" => "fa-flag-checkered",
                "REJECT_REPORT" => "fa-flag",
                "APPROVE_PARTNER" => "fa-handshake",
                "REJECT_PARTNER" => "fa-handshake-slash",
                "CREATE_CATEGORY" => "fa-plus-circle",
                "UPDATE_CATEGORY" => "fa-edit",
                "DELETE_CATEGORY" => "fa-trash",
                "APPROVE_AD_PACKAGE" => "fa-check-circle",
                "UPDATE_SECURITY_SETTINGS" => "fa-lock",
                // User actions
                "CREATE_POST" => "fa-plus-circle",
                "CREATE_COMMENT" => "fa-comment",
                "LIKE_COMMENT" => "fa-thumbs-up",
                "DISLIKE_COMMENT" => "fa-thumbs-down",
                "FOLLOW_USER" => "fa-user-plus",
                "UNFOLLOW_USER" => "fa-user-minus",
                "DELETE_OWN_POST" => "fa-trash-alt",
                _ => "fa-info-circle"
            };
        }

        public static string GetActionColor(string actionType)
        {
            return actionType switch
            {
                "BAN_USER" => "danger",
                "UNBAN_USER" => "success",
                "CHANGE_USER_ROLE" => "info",
                "CHANGE_POST_STATUS" => "info",
                "UPDATE_POST" => "primary",
                "DELETE_POST" => "danger",
                "RESTORE_POST" => "success",
                "APPROVE_POST" => "success",
                "REJECT_POST" => "warning",
                "ARCHIVE_POST" => "secondary",
                "DELETE_COMMENT" => "danger",
                "RESOLVE_REPORT" => "success",
                "REJECT_REPORT" => "warning",
                "APPROVE_PARTNER" => "success",
                "REJECT_PARTNER" => "danger",
                "CREATE_CATEGORY" => "success",
                "UPDATE_CATEGORY" => "primary",
                "DELETE_CATEGORY" => "danger",
                "APPROVE_AD_PACKAGE" => "success",
                "UPDATE_SECURITY_SETTINGS" => "warning",
                // User actions
                "CREATE_POST" => "success",
                "CREATE_COMMENT" => "primary",
                "LIKE_COMMENT" => "success",
                "DISLIKE_COMMENT" => "warning",
                "FOLLOW_USER" => "info",
                "UNFOLLOW_USER" => "secondary",
                "DELETE_OWN_POST" => "danger",
                _ => "secondary"
            };
        }
    }
}
