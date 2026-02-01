using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Categories.CreateCategory
{
    public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, long>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateCategoryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<long> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            // Check if slug already exists
            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == request.Slug, cancellationToken);
            
            if (existing != null)
            {
                throw new InvalidOperationException($"Category with slug '{request.Slug}' already exists");
            }

            var category = new Category
            {
                Name = request.Name,
                Slug = request.Slug,
                Icon = request.Icon
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            var adminId = _currentUser.UserId;
            if (adminId == 0 && _currentUser.IsConfigAdmin())
            {
                var adminUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Admin"))
                    .OrderBy(u => u.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (adminUser != null)
                {
                    adminId = adminUser.UserId;
                }
            }

            // Log action
            if (adminId > 0)
            {
                var adminLog = new AdminLog
                {
                    AdminId = adminId,
                    ActionType = "CREATE_CATEGORY",
                    TargetTable = "Categories",
                    TargetId = category.CategoryId,
                    Note = $"Created category: {category.Name}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return category.CategoryId;
        }
    }
}
