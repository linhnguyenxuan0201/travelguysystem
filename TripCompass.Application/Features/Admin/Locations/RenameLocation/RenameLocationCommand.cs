using MediatR;

namespace TripCompass.Application.Features.Admin.Locations.RenameLocation
{
    public class RenameLocationCommand : IRequest<bool>
    {
        public string OldLocation { get; set; } = null!;
        public string NewLocation { get; set; } = null!;
    }
}
