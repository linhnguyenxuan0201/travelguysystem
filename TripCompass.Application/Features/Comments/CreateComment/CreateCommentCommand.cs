using MediatR;

namespace TripCompass.Application.Features.Comments.CreateComment
{
    public record CreateCommentCommand(long PostId, long UserId, string Content, long? ParentCommentId = null) : IRequest;
}
