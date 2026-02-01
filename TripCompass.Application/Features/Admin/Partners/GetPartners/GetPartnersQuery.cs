using MediatR;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Partners.GetPartners
{
    public class GetPartnersQuery : IRequest<PaginatedList<PartnerListItemDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        public string? SearchTerm { get; set; }
        public bool? IsApproved { get; set; }
        public string? BusinessType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
