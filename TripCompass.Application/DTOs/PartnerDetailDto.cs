namespace TripCompass.Application.DTOs
{
    public class PartnerDetailDto
    {
        // Partner Information
        public long PartnerId { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public DateTime UserCreatedAt { get; set; }
        public int UserReputationScore { get; set; }
        public int UserReputationLevel { get; set; }
        
        // Business Information
        public string StoreName { get; set; } = null!;
        public string BusinessType { get; set; } = null!;
        public string RepresentativeName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string BusinessAddress { get; set; } = null!;
        
        // Bank Information
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountHolderName { get; set; } = null!;
        
        // Legal Information
        public string IdNumber { get; set; } = null!;
        public string? TaxId { get; set; }
        
        // Service Description
        public string? ServiceDescription { get; set; }
        
        // Status
        public bool IsApproved { get; set; }
        public string StatusDisplay { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Statistics
        public PartnerStatisticsDto Statistics { get; set; } = new();
        
        // Related Data
        public List<PartnerPostDto> Posts { get; set; } = new();
        public List<PartnerBookingDto> Bookings { get; set; } = new();
        public List<PartnerAdPackageDto> AdPackages { get; set; } = new();
    }
    
    public class PartnerStatisticsDto
    {
        public int TotalPosts { get; set; }
        public int PublishedPosts { get; set; }
        public int PendingPosts { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int ProcessingBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public int TotalAdPackages { get; set; }
        public int ActiveAdPackages { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
    }
    
    public class PartnerPostDto
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string? Location { get; set; }
        public string Status { get; set; } = null!;
        public string StatusDisplay { get; set; } = null!;
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public class PartnerBookingDto
    {
        public long BookingId { get; set; }
        public long PostId { get; set; }
        public string PostTitle { get; set; } = null!;
        public long CustomerUserId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;
        public DateTime BookedAt { get; set; }
        public DateTime? VisitDate { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public decimal? CommissionAmount { get; set; }
    }
    
    public class PartnerAdPackageDto
    {
        public long PartnerDiscountCodeId { get; set; }
        public string Code { get; set; } = null!;
        public int PercentOff { get; set; }
        public string Purpose { get; set; } = null!;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public string StatusDisplay { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
