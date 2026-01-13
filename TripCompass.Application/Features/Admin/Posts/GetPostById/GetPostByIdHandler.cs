using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Posts.GetPostById
{
    public class GetPostByIdHandler : IRequestHandler<GetPostByIdQuery, PostDetailDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetPostByIdHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PostDetailDto?> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
                .Include(p => p.PostImages)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PostId == request.PostId, cancellationToken);

            if (post == null) return null;

            // Get user avatar separately
            var userAvatar = await _context.UserAvatars
                .Where(ua => ua.UserId == post.UserId && ua.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            return new PostDetailDto
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                Location = post.Location,
                Status = post.Status,
                UserId = post.UserId,
                AuthorName = post.User.UserName,
                AuthorAvatar = userAvatar?.AvatarUrl,
                AuthorEmail = post.User.Email,
                ViewCount = post.ViewCount,
                LikeCount = post.LikeCount,
                DislikeCount = post.DislikeCount,
                CreatedAt = post.CreatedAt,
                PublishedAt = post.PublishedAt,
                
                // SEO
                Slug = post.Slug,
                SeoTitle = post.SeoTitle,
                MetaDescription = post.MetaDescription,
                CanonicalUrl = post.CanonicalUrl,
                IsIndexable = post.IsIndexable,

                // Flags
                IsFeatured = post.IsFeatured,
                IsTrending = post.IsTrending,
                IsPinned = post.IsPinned,

                // Moderation
                ModerationNote = post.ModerationNote,

                // Soft Delete
                IsDeleted = post.IsDeleted,
                DeletedAt = post.DeletedAt,

                Categories = post.PostCategories.Select(pc => pc.Category.Name).ToList(),
                Images = post.PostImages.Select(pi => pi.ImageUrl).ToList()
            };
        }
    }
}
