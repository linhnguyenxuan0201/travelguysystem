using MediatR;

namespace TripCompass.Application.Features.Admin.Partners.RejectPartner
{
    public class RejectPartnerCommand : IRequest<bool>
    {
        public long PartnerId { get; set; }
        public string? RejectionNote { get; set; }
    }
}
