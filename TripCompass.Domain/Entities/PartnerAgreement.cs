namespace TripCompass.Domain.Entities
{
    public class PartnerAgreement
    {
        public long AgreementId { get; set; }
        public long UserId { get; set; }
        public string AgreementVersion { get; set; } = null!; // v1.0, v1.1, etc.
        public DateTime AgreedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        
        // Navigation
        public User User { get; set; } = null!;
    }
}
