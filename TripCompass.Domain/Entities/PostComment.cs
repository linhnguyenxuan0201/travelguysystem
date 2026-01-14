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

        public static PostComment Create(long postId, long userId, string content, long? parentCommentId = null)
            => new(postId, userId, content, parentCommentId);

        private PostComment(long postId, long userId, string content, long? parentCommentId = null)
        {
            PostId = postId;
            UserId = userId;
            Content = content;
            ParentCommentId = parentCommentId;
        }

        public void Delete() => IsDeleted = true;
    }

}
