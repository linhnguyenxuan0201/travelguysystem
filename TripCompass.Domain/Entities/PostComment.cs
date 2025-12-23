using System;
using System.Collections.Generic;
using System.Text;

namespace TripCompass.Domain.Entities
{
    public class PostComment : BaseEntity
    {
        public long PostId { get; private set; }
        public long UserId { get; private set; }
        public long? ParentCommentId { get; private set; }
        public string Content { get; private set; } = null!;
        public bool IsDeleted { get; private set; }

        private PostComment() { }

        public static PostComment Create(long postId, long userId, string content)
            => new(postId, userId, content);

        private PostComment(long postId, long userId, string content)
        {
            PostId = postId;
            UserId = userId;
            Content = content;
        }

        public void Delete() => IsDeleted = true;
    }

}
