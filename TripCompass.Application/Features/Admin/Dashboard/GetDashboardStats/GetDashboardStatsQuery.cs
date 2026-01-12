using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Dashboard.GetDashboardStats
{
    public class GetDashboardStatsQuery : IRequest<DashboardStatsDto>
    {
    }
}
