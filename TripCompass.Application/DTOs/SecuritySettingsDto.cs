namespace TripCompass.Application.DTOs
{
    public class SecuritySettingsDto
    {
        public PasswordPolicyDto PasswordPolicy { get; set; } = new();
        public SystemSecurityDto SystemSecurity { get; set; } = new();
    }

    public class PasswordPolicyDto
    {
        public int MinLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialChar { get; set; } = true;
        public int MaxFailedAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 30;
    }

    public class SystemSecurityDto
    {
        public bool RequireEmailVerification { get; set; } = true;
        public bool EnableTwoFactorAuth { get; set; } = false;
        public int SessionTimeoutMinutes { get; set; } = 60;
        public bool EnableIpWhitelist { get; set; } = false;
        public bool EnableContentModeration { get; set; } = true;
        public bool EnableAutoBan { get; set; } = false;
        public int AutoBanReportThreshold { get; set; } = 5;
    }
}
