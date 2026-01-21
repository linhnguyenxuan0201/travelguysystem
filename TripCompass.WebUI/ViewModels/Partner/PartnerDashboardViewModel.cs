using System;
using System.Collections.Generic;

namespace TripCompass.WebUI.ViewModels.Partner
{
    public class PartnerDashboardViewModel
    {
        public string ShopName { get; set; } = "ShopAdmin";

        // KPIs
        public decimal TotalRevenue { get; set; }
        public int NewOrders { get; set; }
        public int WalletBalance { get; set; }
        public int ActiveDiscountCodes { get; set; }

        public List<MonthlyRevenuePoint> MonthlyRevenue { get; set; } = new();
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
        public List<DiscountCodeItem> DiscountCodes { get; set; } = new();
        public List<WalletActivityItem> WalletActivities { get; set; } = new();
    }

    public class MonthlyRevenuePoint
    {
        public int Month { get; set; } // 1-12
        public decimal Amount { get; set; }
    }

    public class RecentOrderItem
    {
        public long BookingId { get; set; }
        public long PostId { get; set; }
        public string PostTitle { get; set; } = "Booking";
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Processing";
        public DateTime BookedAt { get; set; }
    }

    public class DiscountCodeItem
    {
        public long Id { get; set; }
        public string Code { get; set; } = "";
        public int PercentOff { get; set; }
        public string Purpose { get; set; } = "";
        public bool IsActive { get; set; } // false = Chờ duyệt, true = Đã duyệt và hoạt động
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DiscountCodesListViewModel
    {
        public List<DiscountCodeItem> Codes { get; set; } = new();
    }

    public class WalletActivityItem
    {
        public string Label { get; set; } = "";
        public DateTime At { get; set; }
        public int Amount { get; set; }
    }
}

