using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Security.GetSecuritySettings
{
    public class GetSecuritySettingsQuery : IRequest<SecuritySettingsDto>
    {
    }
}
