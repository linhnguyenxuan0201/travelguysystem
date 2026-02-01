using MediatR;

namespace TripCompass.Application.Features.Admin.Comments.DeleteComment
{
    public class DeleteCommentCommand : IRequest<bool>
    {
        public long CommentId { get; set; }
    }
}
