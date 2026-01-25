namespace TripCompass.Application.Common.Security
{
    public class AdminConfig
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }
}
