using MediatR;
using TripCompass.Application.Common.Models;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Posts.GetPosts
{
    public class GetPostsQuery : IRequest<PaginatedList<PostDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; }
        public PostStatus? Status { get; set; }
        public long? CategoryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
