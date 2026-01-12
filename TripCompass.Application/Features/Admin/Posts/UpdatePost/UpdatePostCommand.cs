using MediatR;

namespace TripCompass.Application.Features.Admin.Posts.UpdatePost
{
    public class UpdatePostCommand : IRequest<bool>
    {
        public long PostId { get; set; }
        
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        
        // SEO
        public string? Slug { get; set; }
        public string? SeoTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? CanonicalUrl { get; set; }
        public bool IsIndexable { get; set; }

        // Flags
        public bool IsFeatured { get; set; }
        public bool IsTrending { get; set; }
        public bool IsPinned { get; set; }
    }
}
