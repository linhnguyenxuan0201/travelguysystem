using MediatR;

namespace TripCompass.Application.Features.Admin.AdPackages.ApproveAdPackage
{
    public class ApproveAdPackageCommand : IRequest<bool>
    {
        public long PartnerDiscountCodeId { get; set; }
    }
}
