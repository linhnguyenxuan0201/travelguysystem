using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.ActivityHistory.GetActivityHistory
{
    public class GetActivityHistoryQuery : IRequest<(List<ActivityHistoryDto> Items, int TotalCount)>
    {
        public long? AdminId { get; set; }
        public string? ActionType { get; set; }
        public string? TargetTable { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
