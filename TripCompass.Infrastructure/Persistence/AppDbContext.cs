using Microsoft.EntityFrameworkCore;
using TripCompass.Domain.Entities;

namespace TripCompass.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
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
            // APPLY OTHER CONFIGS
            // ========================
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
