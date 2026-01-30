using System.Threading;
using System.Threading.Tasks;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        // Core repositories
        IUserRepository Users { get; }
        ICommentRepository Comments { get; }
        IWalletRepository Wallets { get; }

        // ✅ Avatar repository (ADD)
        IUserAvatarRepository UserAvatars { get; }

        // Save changes
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
