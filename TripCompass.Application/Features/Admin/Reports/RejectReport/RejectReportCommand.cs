using MediatR;

namespace TripCompass.Application.Features.Admin.Reports.RejectReport
{
    public class RejectReportCommand : IRequest<bool>
    {
        public long ReportId { get; set; }
        public string? RejectionNote { get; set; }
    }
}
