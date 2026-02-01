using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Categories.GetCategories
{
    public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<CategoryListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCategoriesHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryListItemDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Categories
                .AsNoTracking()
                .AsQueryable();

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    c.Slug.ToLower().Contains(term));
            }

            var categories = await query
                .Select(c => new CategoryListItemDto
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    Slug = c.Slug,
                    Icon = c.Icon,
                    PostCount = c.PostCategories.Count(pc => !pc.Post.IsDeleted)
                })
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            return categories;
        }
    }
}
