using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Enums
{
    public enum PostStatus
    {
        Pending = 0,   // 🟡 Chờ duyệt
        Approved = 1,  // 🟢 Đã duyệt
        Rejected = 2   // 🔴 Từ chối
    }
}
