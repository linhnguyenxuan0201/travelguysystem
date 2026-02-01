using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Security.UpdateSecuritySettings
{
    public class UpdateSecuritySettingsCommand : IRequest<bool>
    {
        public PasswordPolicyDto PasswordPolicy { get; set; } = new();
        public SystemSecurityDto SystemSecurity { get; set; } = new();
    }
}
