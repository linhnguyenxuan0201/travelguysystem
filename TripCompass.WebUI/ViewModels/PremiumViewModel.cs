namespace TripCompass.WebUI.ViewModels
{
    public class PremiumViewModel
    {
        public string? CurrentPlan { get; set; }
        public bool IsAuthenticated { get; set; }
        public bool IsPremium { get; set; }
        public DateTime? PremiumExpiresAt { get; set; }
    }
}
