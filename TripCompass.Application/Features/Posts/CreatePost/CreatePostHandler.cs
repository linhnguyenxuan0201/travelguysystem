using MediatR;
using TripCompass.Application.Common;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Posts.CreatePost
{
    public class CreatePostHandler : IRequestHandler<CreatePostCommand, long>
    {
        private readonly IApplicationDbContext _context;

        public CreatePostHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<long> Handle(CreatePostCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null) throw new Exception("User not found");
            
            // Business Rule: Banned user cannot create post
            if (user.IsBanned) throw new UnauthorizedAccessException("User is banned and cannot create posts.");

            var post = new Post
            {
                UserId = request.UserId,
                Title = request.Title,
                Content = request.Content,
                Location = request.Location,
                CreatedAt = DateTime.UtcNow,
                Status = Domain.Enums.PostStatus.Pending
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync(cancellationToken);

            // Log activity
            await ActivityLogger.LogActivityAsync(
                _context,
                request.UserId,
                "CREATE_POST",
                "Posts",
                post.PostId,
                $"Created post: {post.Title}");

            return post.PostId;
        }
    }
}
