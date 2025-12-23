using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // ✅ BẮT BUỘC
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.Infrastructure.Repositories
{
    public class UserAvatarRepository : IUserAvatarRepository
    {
        private readonly AppDbContext _db;

        public UserAvatarRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<UserAvatar?> GetActiveByUserIdAsync(long userId)
        {
            return await _db.UserAvatars
                .FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
        }

        public async Task DeactivateAllAsync(long userId)
        {
            var avatars = await _db.UserAvatars
                .Where(x => x.UserId == userId && x.IsActive)
                .ToListAsync();

            foreach (var a in avatars)
                a.IsActive = false;
        }

        public async Task AddAsync(UserAvatar avatar)
        {
            _db.UserAvatars.Add(avatar);
        }
    }
}
