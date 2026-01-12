using MediatR;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Posts.UpdatePost
{
    public class UpdatePostHandler : IRequestHandler<UpdatePostCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public UpdatePostHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts.FindAsync(new object[] { request.PostId }, cancellationToken);

            if (post == null) return false;

            post.Title = request.Title;
            post.Content = request.Content;
            
            // SEO
            post.Slug = request.Slug;
            post.SeoTitle = request.SeoTitle;
            post.MetaDescription = request.MetaDescription;
            post.CanonicalUrl = request.CanonicalUrl;
            post.IsIndexable = request.IsIndexable;

            // Flags
            post.IsFeatured = request.IsFeatured;
            post.IsTrending = request.IsTrending;
            post.IsPinned = request.IsPinned;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
