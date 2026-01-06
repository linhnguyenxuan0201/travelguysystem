namespace TripCompass.WebUI.ViewModels
{
    public class UserDropdownViewModel
    {
        public string UserName { get; set; } = "";
        public string? AvatarUrl { get; set; }

        public int ReputationLevel { get; set; }
        public int ReputationScore { get; set; }

        public int WalletBalance { get; set; }
    }
}
