using Microsoft.EntityFrameworkCore;
using TripCompass.Domain.Entities;

using TripCompass.Application.Interfaces;

namespace TripCompass.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IApplicationDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ========================
        // DB SETS
        // ========================
        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<PostComment> PostComments => Set<PostComment>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
        public DbSet<Wallet> Wallets => Set<Wallet>();

        // ✅ USER AVATAR (TABLE RIÊNG)
        public DbSet<UserAvatar> UserAvatars => Set<UserAvatar>();
        public DbSet<UserPlan> UserPlans { get; set; }
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<PostCategory> PostCategories => Set<PostCategory>();
        public DbSet<PostImage> PostImages => Set<PostImage>();
        public DbSet<CoinTransaction> CoinTransactions => Set<CoinTransaction>();
        public DbSet<PostReaction> PostReactions => Set<PostReaction>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<AdminLog> AdminLogs => Set<AdminLog>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================
            // USER AVATAR CONFIG
            // ========================
            modelBuilder.Entity<UserAvatar>(entity =>
            {
                // ✅ PRIMARY KEY (BẮT BUỘC)
                entity.HasKey(x => x.UserAvatarId);

                entity.Property(x => x.AvatarUrl)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

                entity.Property(x => x.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // FK → USER
                entity.HasOne(x => x.User)
                      .WithMany() // hoặc .WithMany(u => u.Avatars) nếu bạn thêm collection
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================
            // COIN TRANSACTION
            // ========================
            modelBuilder.Entity<CoinTransaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // ========================
            // POST REACTION
            // ========================
            modelBuilder.Entity<PostReaction>(entity =>
            {
                entity.HasKey(e => e.ReactionId);
                entity.HasOne(e => e.Post)
                      .WithMany()
                      .HasForeignKey(e => e.PostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
                
                // Unique Constraint
                entity.HasIndex(e => new { e.PostId, e.UserId }).IsUnique();
            });

            // ========================
            // REPORT
            // ========================
            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(e => e.ReportId);
                entity.HasOne(e => e.Reporter)
                      .WithMany()
                      .HasForeignKey(e => e.ReporterId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.Resolver)
                      .WithMany()
                      .HasForeignKey(e => e.ResolvedBy)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // ========================
            // ADMIN LOG
            // ========================
            modelBuilder.Entity<AdminLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.HasOne(e => e.Admin)
                      .WithMany()
                      .HasForeignKey(e => e.AdminId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // ========================
            // POST
            // ========================
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================
            // APPLY OTHER CONFIGS
            // ========================
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
