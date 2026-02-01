using MediatR;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Reports.GetReports
{
    public class GetReportsQuery : IRequest<PaginatedList<ReportListItemDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        public string? SearchTerm { get; set; }
        public int? Status { get; set; } // 0 Pending | 1 Resolved | 2 Rejected
        public string? TargetType { get; set; } // POST | COMMENT | USER
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
