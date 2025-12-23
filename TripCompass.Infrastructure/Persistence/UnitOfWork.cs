using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;

namespace TripCompass.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        public IUserRepository Users { get; }
        public ICommentRepository Comments { get; }
        public IWalletRepository Wallets { get; }

        // ✅ ADD: User Avatars
        public IUserAvatarRepository UserAvatars { get; }

        public UnitOfWork(
            AppDbContext db,
            IUserRepository users,
            ICommentRepository comments,
            IWalletRepository wallets,
            IUserAvatarRepository userAvatars)
        {
            _db = db;
            Users = users;
            Comments = comments;
            Wallets = wallets;
            UserAvatars = userAvatars;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
