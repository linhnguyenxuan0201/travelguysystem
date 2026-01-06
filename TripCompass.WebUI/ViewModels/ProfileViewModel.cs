namespace TripCompass.WebUI.ViewModels
{
    public class ProfileViewModel
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;

        public int ReputationScore { get; set; }
        public int ReputationLevel { get; set; }
        public decimal WalletBalance { get; set; }

        public DateTime JoinedAt { get; set; }

        // UI only
        public string AccountType { get; set; } = "Free";
        public bool IsActive { get; set; } = true;
        // RIGHT (PLAN)
        public string CurrentPlan { get; set; } = "Free";
        public List<string> CurrentPlanFeatures { get; set; } = new();
        public string? NextPlan { get; set; }
        public string? UpgradeBonus { get; set; }
    }
}

