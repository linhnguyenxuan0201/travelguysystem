using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Application.Features.Comments.CreateComment
{
    public record CreateCommentCommand(long PostId, long UserId, string Content);

}
