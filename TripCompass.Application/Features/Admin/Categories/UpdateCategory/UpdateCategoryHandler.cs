using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Admin.Categories.UpdateCategory
{
    public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateCategoryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<bool> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
            
            if (category == null) return false;

            // Check if slug already exists (excluding current category)
            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == request.Slug && c.CategoryId != request.CategoryId, cancellationToken);
            
            if (existing != null)
            {
                throw new InvalidOperationException($"Category with slug '{request.Slug}' already exists");
            }

            category.Name = request.Name;
            category.Slug = request.Slug;
            category.Icon = request.Icon;

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
                    ActionType = "UPDATE_CATEGORY",
                    TargetTable = "Categories",
                    TargetId = category.CategoryId,
                    Note = $"Updated category: {category.Name}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
