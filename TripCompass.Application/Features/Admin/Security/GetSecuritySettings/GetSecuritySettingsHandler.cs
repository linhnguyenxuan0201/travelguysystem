using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Security.GetSecuritySettings
{
    public class GetSecuritySettingsHandler : IRequestHandler<GetSecuritySettingsQuery, SecuritySettingsDto>
    {
        public Task<SecuritySettingsDto> Handle(GetSecuritySettingsQuery request, CancellationToken cancellationToken)
        {
            // For now, return default settings
            // In production, these would be loaded from database or configuration
            var settings = new SecuritySettingsDto
            {
                PasswordPolicy = new PasswordPolicyDto
                {
                    MinLength = 8,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigit = true,
                    RequireSpecialChar = true,
                    MaxFailedAttempts = 5,
                    LockoutDurationMinutes = 30
                },
                SystemSecurity = new SystemSecurityDto
                {
                    RequireEmailVerification = true,
                    EnableTwoFactorAuth = false,
                    SessionTimeoutMinutes = 60,
                    EnableIpWhitelist = false,
                    EnableContentModeration = true,
                    EnableAutoBan = false,
                    AutoBanReportThreshold = 5
                }
            };

            return Task.FromResult(settings);
        }
    }
}
