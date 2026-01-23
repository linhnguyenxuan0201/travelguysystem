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
        public string PaymentMethod { get; set; } = "Cash"; // Cash/Online
        public string PaymentStatus { get; set; } = "Pending"; // Pending/Paid/Failed
        public DateTime? PaidAt { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? PaymentRef { get; set; } // mã giao dịch / reference từ ngân hàng
        public long? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        
        // Commission tracking
        public bool CommissionDeducted { get; set; } = false; // Đã trừ phí hoa hồng chưa
        public decimal? CommissionAmount { get; set; } // Số tiền phí đã trừ
        public bool CommissionPaid { get; set; } = false; // Đã trả phí hoa hồng cho admin chưa
        public DateTime? CommissionPaidAt { get; set; } // Thời gian trả phí
        public string? CommissionPaymentRef { get; set; } // Mã giao dịch thanh toán phí
        
        // Refund tracking
        public bool Refunded { get; set; } = false; // Đã hoàn tiền chưa
        public decimal? RefundAmount { get; set; } // Số tiền đã hoàn
        public DateTime? RefundedAt { get; set; } // Thời gian hoàn tiền
        public string? RefundReason { get; set; } // Lý do hoàn tiền
    }
}

