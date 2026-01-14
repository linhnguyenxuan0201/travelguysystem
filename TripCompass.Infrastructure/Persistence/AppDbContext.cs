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
        public DbSet<UserFollow> UserFollows => Set<UserFollow>();
        public DbSet<CommentReaction> CommentReactions => Set<CommentReaction>();


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
            // POST COMMENT
            // ========================
            modelBuilder.Entity<PostComment>(entity =>
            {
                // Map Id property to CommentId column
                entity.Property(e => e.Id)
                      .HasColumnName("CommentId");
            });

            // ========================
            // USER FOLLOW
            // ========================
            modelBuilder.Entity<UserFollow>(entity =>
            {
                entity.HasKey(e => e.FollowId);
                
                entity.HasOne(e => e.Follower)
                      .WithMany()
                      .HasForeignKey(e => e.FollowerId)
                      .OnDelete(DeleteBehavior.NoAction);
                
                entity.HasOne(e => e.Following)
                      .WithMany()
                      .HasForeignKey(e => e.FollowingId)
                      .OnDelete(DeleteBehavior.NoAction);
                
                // Unique constraint: một user chỉ follow một user khác một lần
                entity.HasIndex(e => new { e.FollowerId, e.FollowingId }).IsUnique();
                
                // Không cho phép follow chính mình
                entity.HasCheckConstraint("CK_UserFollow_NotSelf", "[FollowerId] <> [FollowingId]");
            });

            // ========================
            // COMMENT REACTION
            // ========================
            modelBuilder.Entity<CommentReaction>(entity =>
            {
                entity.HasKey(e => e.ReactionId);
                
                entity.HasOne(e => e.Comment)
                      .WithMany()
                      .HasForeignKey(e => e.CommentId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
                
                // Unique constraint: một user chỉ react một comment một lần
                entity.HasIndex(e => new { e.CommentId, e.UserId }).IsUnique();
            });

            // ========================
            // APPLY OTHER CONFIGS
            // ========================
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
