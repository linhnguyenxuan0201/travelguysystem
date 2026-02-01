using MediatR;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Partners.GetPartnerDetail
{
    public class GetPartnerDetailQuery : IRequest<PartnerDetailDto>
    {
        public long PartnerId { get; set; }
    }
}
