using Microsoft.EntityFrameworkCore;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Post> Posts { get; }
        DbSet<PostComment> PostComments { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<EmailOtp> EmailOtps { get; }
        DbSet<Wallet> Wallets { get; }
        DbSet<UserAvatar> UserAvatars { get; }
        DbSet<UserPlan> UserPlans { get; }
        DbSet<Category> Categories { get; }
        DbSet<PostCategory> PostCategories { get; }
        DbSet<PostImage> PostImages { get; }
        DbSet<CoinTransaction> CoinTransactions { get; }
        DbSet<PostReaction> PostReactions { get; }
        DbSet<Report> Reports { get; }
        DbSet<AdminLog> AdminLogs { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
