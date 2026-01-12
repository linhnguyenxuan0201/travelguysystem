using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Enums
{
    public enum PostStatus
    {
        Draft = 0,     // 📝 Nháp
        Pending = 1,   // 🟡 Chờ duyệt
        Published = 2, // 🟢 Đã xuất bản
        Rejected = 3,  // 🔴 Từ chối
        Archived = 4   // 📦 Lưu trữ
    }
}
