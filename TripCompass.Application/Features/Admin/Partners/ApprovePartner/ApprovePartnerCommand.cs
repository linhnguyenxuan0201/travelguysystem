using MediatR;

namespace TripCompass.Application.Features.Admin.Partners.ApprovePartner
{
    public class ApprovePartnerCommand : IRequest<bool>
    {
        public long PartnerId { get; set; }
        public string? ApprovalNote { get; set; }
    }
}
