using MediatR;

namespace TripCompass.Application.Features.Admin.Reports.ResolveReport
{
    public class ResolveReportCommand : IRequest<bool>
    {
        public long ReportId { get; set; }
        public string? ResolutionNote { get; set; }
    }
}
