using System;
using System.Collections.Generic;

namespace TripCompass.WebUI.ViewModels.Partner
{
    public class CommissionViewModel
    {
        public List<CommissionBookingItem> Bookings { get; set; } = new();
        public int TotalCommission { get; set; }
        public int UnpaidCommission { get; set; }
    }

    public class CommissionBookingItem
    {
        public long BookingId { get; set; }
        public long PostId { get; set; }
        public string PostTitle { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int CommissionAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentStatus { get; set; } = "Pending";
        public DateTime BookedAt { get; set; }
        public bool CommissionPaid { get; set; }
        public DateTime? CommissionPaidAt { get; set; }
    }
}
