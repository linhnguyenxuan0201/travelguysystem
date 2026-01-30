using System;
using System.ComponentModel.DataAnnotations;

namespace TripCompass.Domain.Entities
{
    public class PremiumOrder
    {
        // EF không tự bắt được khóa chính vì tên không theo convention (PremiumOrderId),
        // nên cần đánh dấu rõ ràng bằng [Key]
        [Key]
        public long OrderId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; } = null!;

        public string PlanCode { get; set; } = null!; // Pro / Enterprise
        public string PlanType { get; set; } = null!; // monthly / yearly
        public decimal Amount { get; set; }

        public string Status { get; set; } = "Pending"; // Pending / Paid / Failed / Cancelled
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public string? PaymentRef { get; set; } // Mã giao dịch từ ngân hàng
        public string? TransactionId { get; set; } // Transaction ID từ webhook
    }
}
