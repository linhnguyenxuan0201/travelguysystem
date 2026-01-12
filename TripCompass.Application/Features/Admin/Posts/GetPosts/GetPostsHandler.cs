using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common.Models;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Posts.GetPosts
{
    public class GetPostsHandler : IRequestHandler<GetPostsQuery, PaginatedList<PostDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetPostsHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<PostDto>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories)
                .ThenInclude(pc => pc.Category)
                .AsNoTracking()
                .AsQueryable();

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(term) || 
                    p.PostId.ToString() == term);
            }

            // Filter by Status
            if (request.Status.HasValue)
            {
                query = query.Where(p => p.Status == request.Status.Value);
            }

            // Filter by Category
            if (request.CategoryId.HasValue)
            {
                query = query.Where(p => p.PostCategories.Any(pc => pc.CategoryId == request.CategoryId.Value));
            }

            // Filter by Date
            if (request.FromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);
            }

            // Order By (Default: Newest first)
            query = query.OrderByDescending(p => p.CreatedAt);

            // Get posts with pagination
            var postsList = await PaginatedList<Post>.CreateAsync(query, request.PageNumber, request.PageSize, cancellationToken);

            // Get user IDs for avatar lookup
            var userIds = postsList.Items.Select(p => p.UserId).Distinct().ToList();
            var userAvatars = await _context.UserAvatars
                .Where(ua => userIds.Contains(ua.UserId) && ua.IsActive)
                .ToListAsync(cancellationToken);

            // Project to DTOs
            var postDtos = postsList.Items.Select(p =>
            {
                var avatar = userAvatars.FirstOrDefault(ua => ua.UserId == p.UserId);
                return new PostDto
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    AuthorName = p.User.UserName,
                    AuthorAvatar = avatar?.AvatarUrl,
                    CategoryName = p.PostCategories.Any() 
                        ? p.PostCategories.First().Category.Name 
                        : "Uncategorized",
                    Status = p.Status,
                    ViewCount = p.ViewCount,
                    CreatedAt = p.CreatedAt,
                    PublishedAt = p.PublishedAt,
                    IsFeatured = p.IsFeatured,
                    IsTrending = p.IsTrending
                };
            }).ToList();

            return new PaginatedList<PostDto>(
                postDtos,
                postsList.TotalCount,
                postsList.PageNumber,
                request.PageSize);
        }
    }
}
