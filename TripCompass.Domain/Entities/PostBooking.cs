using System;

namespace TripCompass.Domain.Entities
{
    public class PostBooking
    {
        public long BookingId { get; set; }

        // Bài viết (dịch vụ) được đặt
        public long PostId { get; set; }

        // Đối tác (chủ bài viết)
        public long PartnerUserId { get; set; }

        // Người đặt
        public long CustomerUserId { get; set; }

        // Thông tin liên hệ người đặt (nhập trong form)
        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;

        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        // Thời gian đến (datetime)
        public DateTime? VisitDate { get; set; }

        // Số người
        public int Quantity { get; set; } = 1;
        public decimal TotalAmount { get; set; } = 0;

        public string Status { get; set; } = "Processing"; // Processing/Completed/Cancelled

        // Ưu đãi / mã giảm giá (nếu có)
        public string? PromoCode { get; set; }

        // Optional note
        public string? Note { get; set; }

        // Payment tracking
        public string PaymentStatus { get; set; } = "Pending"; // Pending/Paid/Failed
        public DateTime? PaidAt { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? PaymentRef { get; set; } // mã giao dịch / reference từ ngân hàng
        public long? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }
}

