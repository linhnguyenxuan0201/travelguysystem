namespace TripCompass.Application.DTOs
{
    public class AdPackageListItemDto
    {
        public long PartnerDiscountCodeId { get; set; }
        public long PartnerUserId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string Code { get; set; } = null!;
        public int PercentOff { get; set; }
        public string Purpose { get; set; } = null!;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public string StatusDisplay { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
