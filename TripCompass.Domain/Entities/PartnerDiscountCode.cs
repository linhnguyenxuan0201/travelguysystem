using System;

namespace TripCompass.Domain.Entities
{
    public class PartnerDiscountCode
    {
        public long PartnerDiscountCodeId { get; set; }
        public long PartnerUserId { get; set; } // userId của đối tác

        public string Code { get; set; } = null!;
        public int PercentOff { get; set; } // 1-100
        public string Purpose { get; set; } = null!; // dùng để làm gì
        public DateTime? ExpiryDate { get; set; } // hạn sử dụng

        public bool IsActive { get; set; } = false; // false = Chờ duyệt, true = Đã duyệt và hoạt động
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

