using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly AppDbContext _context;

        public PostRepository(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // MY REVIEWS LIST
        // =====================================================
        public async Task<(List<ReviewListItemDto>, int)>
            GetUserReviewsAsync(
                long userId,
                string? keyword,
                long? categoryId,
                int? rating,
                int page,
                int pageSize)
        {
            IQueryable<Post> query = _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId && !p.IsDeleted)
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.PostImages);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    p.Title.Contains(keyword) ||
                    p.Content.Contains(keyword) ||
                    (p.Location != null && p.Location.Contains(keyword)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.PostCategories.Any(pc => pc.CategoryId == categoryId.Value));
            }

            if (rating.HasValue)
            {
                query = query.Where(p => p.ReputationImpact == rating.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
      .OrderByDescending(p => p.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(p => new ReviewListItemDto
      {
          PostId = p.PostId,
          Title = p.Title,
          Location = p.Location,
          Rating = p.ReputationImpact,
          ViewCount = p.ViewCount,
          LikeCount = p.LikeCount,
          CreatedAt = p.CreatedAt,

          // ⭐⭐ DÒNG QUAN TRỌNG NHẤT ⭐⭐
          Status = p.Status,

          Categories = p.PostCategories
              .Select(pc => pc.Category.Name)
              .ToList(),

          ThumbnailUrl = p.PostImages
              .Where(i => i.IsCover)
              .OrderBy(i => i.SortOrder)
              .Select(i => i.ImageUrl)
              .FirstOrDefault()
              ?? "/images/placeholder.jpg"
      })
      .ToListAsync();

            return (items, totalCount);
        }

        // =====================================================
        // 📊 MONTHLY DASHBOARD STATS
        // =====================================================
        public async Task<List<MonthlyStatDto>> GetMonthlyStatsAsync(
            long userId,
            int year)
        {
            return await _context.Posts
                .AsNoTracking()
                .Where(p =>
                    p.UserId == userId &&
                    !p.IsDeleted &&
                    p.CreatedAt.Year == year)
                .GroupBy(p => p.CreatedAt.Month)
                .Select(g => new MonthlyStatDto
                {
                    Year = year,
                    Month = g.Key,
                    ReviewCount = g.Count(),
                    ViewCount = g.Sum(x => x.ViewCount),
                    LikeCount = g.Sum(x => x.LikeCount)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();
        }

        // =====================================================
        // 🧠 HEATMAP ACTIVITY
        // =====================================================
        public async Task<List<HeatmapDto>> GetHeatmapAsync(
            long userId,
            int year)
        {
            return await _context.Posts
                .AsNoTracking()
                .Where(p =>
                    p.UserId == userId &&
                    !p.IsDeleted &&
                    p.CreatedAt.Year == year)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new HeatmapDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();
        }
    }
}
