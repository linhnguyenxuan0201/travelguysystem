using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.AdPackages.GetAdPackages
{
    public class GetAdPackagesQuery : IRequest<List<AdPackageListItemDto>>
    {
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }
}
