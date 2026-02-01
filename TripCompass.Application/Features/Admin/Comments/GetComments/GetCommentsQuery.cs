using MediatR;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.Comments.GetComments
{
    public class GetCommentsQuery : IRequest<PaginatedList<CommentListItemDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        public string? SearchTerm { get; set; }
        public long? PostId { get; set; }
        public long? UserId { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
