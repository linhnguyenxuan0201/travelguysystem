using MediatR;

namespace TripCompass.Application.Features.Admin.Posts.GetPostById
{
    public class GetPostByIdQuery : IRequest<PostDetailDto?>
    {
        public long PostId { get; set; }

        public GetPostByIdQuery(long postId)
        {
            PostId = postId;
        }
    }
}
