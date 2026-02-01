using System.Threading;
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
        DbSet<CommentReaction> CommentReactions { get; }
        DbSet<Report> Reports { get; }
        DbSet<AdminLog> AdminLogs { get; }
        DbSet<PartnerAgreement> PartnerAgreements { get; }
        DbSet<Partner> Partners { get; }
        DbSet<PartnerDiscountCode> PartnerDiscountCodes { get; }
        DbSet<PostBooking> PostBookings { get; }
        DbSet<Notification> Notifications { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
