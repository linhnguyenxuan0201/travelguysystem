using System;
using System.Collections.Generic;

namespace TripCompass.WebUI.ViewModels.Partner
{
    public class PartnerOrdersViewModel
    {
        public List<PartnerOrderItem> Orders { get; set; } = new();
    }

    public class PartnerOrderItem
    {
        public long BookingId { get; set; }
        public string PostTitle { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public DateTime BookedAt { get; set; }
        public string? Note { get; set; }
    }
}

