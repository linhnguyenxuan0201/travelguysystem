using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        /* =========================
           QUERY
        ========================= */

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /* =========================
           COMMAND
        ========================= */

        public async Task AddAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task AssignRoleAsync(User user, string roleName)
        {
            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleName == roleName);

            if (role == null)
                throw new Exception($"Role '{roleName}' not found");

            // tránh add trùng role
            if (user.UserRoles.Any(ur => ur.RoleId == role.RoleId))
                return;

            user.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = role.RoleId
            });

            await _db.SaveChangesAsync();
        }

        /* =========================
           GOOGLE LOGIN
        ========================= */

        public async Task<User> CreateGoogleUserAsync(string email, string name)
        {
            var user = new User(
                userName: name,
                email: email,
                passwordHash: "GOOGLE"
            );

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await AssignRoleAsync(user, "User");

            return user;
        }
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _db.Users.AnyAsync(u => u.Email == email);
        }

    }
}
