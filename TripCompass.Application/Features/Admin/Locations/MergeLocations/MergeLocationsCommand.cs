using MediatR;

namespace TripCompass.Application.Features.Admin.Locations.MergeLocations
{
    public class MergeLocationsCommand : IRequest<bool>
    {
        public string SourceLocation { get; set; } = null!;
        public string TargetLocation { get; set; } = null!;
    }
}
