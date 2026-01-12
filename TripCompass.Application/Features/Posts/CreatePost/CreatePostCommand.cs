using MediatR;

namespace TripCompass.Application.Features.Posts.CreatePost
{
    public class CreatePostCommand : IRequest<long>
    {
        public long UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Location { get; set; }
    }
}
