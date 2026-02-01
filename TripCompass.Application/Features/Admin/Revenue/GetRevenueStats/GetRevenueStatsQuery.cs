using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Revenue.GetRevenueStats
{
    public class GetRevenueStatsQuery : IRequest<RevenueStatsDto>
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
