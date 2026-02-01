using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Locations.GetLocations
{
    public class GetLocationsQuery : IRequest<List<LocationListItemDto>>
    {
        public string? SearchTerm { get; set; }
    }
}
