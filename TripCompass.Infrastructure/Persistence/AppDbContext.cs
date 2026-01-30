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
        public DbSet<PartnerAgreement> PartnerAgreements => Set<PartnerAgreement>();
        public DbSet<Partner> Partners => Set<Partner>();
        public DbSet<PartnerDiscountCode> PartnerDiscountCodes => Set<PartnerDiscountCode>();
        public DbSet<PostBooking> PostBookings => Set<PostBooking>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<PremiumOrder> PremiumOrders => Set<PremiumOrder>();


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
            // PARTNER AGREEMENT
            // ========================
            modelBuilder.Entity<PartnerAgreement>(entity =>
            {
                entity.HasKey(e => e.AgreementId);
                
                entity.Property(e => e.AgreementVersion)
                      .IsRequired()
                      .HasMaxLength(20);
                
                entity.Property(e => e.AgreedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================
            // PARTNER
            // ========================
            modelBuilder.Entity<Partner>(entity =>
            {
                entity.HasKey(e => e.PartnerId);
                
                entity.Property(e => e.StoreName)
                      .IsRequired()
                      .HasMaxLength(200);
                
                entity.Property(e => e.BusinessType)
                      .IsRequired()
                      .HasMaxLength(100);
                
                entity.Property(e => e.RepresentativeName)
                      .IsRequired()
                      .HasMaxLength(100);
                
                entity.Property(e => e.PhoneNumber)
                      .IsRequired()
                      .HasMaxLength(20);
                
                entity.Property(e => e.BusinessAddress)
                      .IsRequired()
                      .HasMaxLength(500);
                
                entity.Property(e => e.BankName)
                      .IsRequired()
                      .HasMaxLength(100);
                
                entity.Property(e => e.AccountNumber)
                      .IsRequired()
                      .HasMaxLength(50);
                
                entity.Property(e => e.AccountHolderName)
                      .IsRequired()
                      .HasMaxLength(100);
                
                entity.Property(e => e.IdNumber)
                      .IsRequired()
                      .HasMaxLength(20);
                
                entity.Property(e => e.TaxId)
                      .HasMaxLength(20);
                
                entity.Property(e => e.ServiceDescription)
                      .HasMaxLength(2000);
                
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                // Unique constraint: một user chỉ có một partner record
                entity.HasIndex(e => e.UserId).IsUnique();
            });

            // ========================
            // PARTNER DISCOUNT CODE
            // ========================
            modelBuilder.Entity<PartnerDiscountCode>(entity =>
            {
                entity.HasKey(e => e.PartnerDiscountCodeId);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Purpose).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PercentOff).IsRequired();
                entity.Property(e => e.ExpiryDate);
                entity.Property(e => e.IsActive).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => new { e.PartnerUserId, e.Code }).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // ========================
            // POST BOOKING
            // ========================
            modelBuilder.Entity<PostBooking>(entity =>
            {
                entity.HasKey(e => e.BookingId);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
                entity.Property(e => e.Note).HasMaxLength(500);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(120);
                entity.Property(e => e.CustomerPhone).IsRequired().HasMaxLength(30);
                entity.Property(e => e.PromoCode).HasMaxLength(30);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(20).HasDefaultValue("Cash");
                entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaymentRef).HasMaxLength(120);
                entity.Property(e => e.BookedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CommissionDeducted).HasDefaultValue(false);
                entity.Property(e => e.CommissionAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CommissionPaid).HasDefaultValue(false);
                entity.Property(e => e.CommissionPaymentRef).HasMaxLength(120);
                entity.Property(e => e.Refunded).HasDefaultValue(false);
                entity.Property(e => e.RefundAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RefundReason).HasMaxLength(500);

                entity.HasOne(e => e.Post)
                      .WithMany()
                      .HasForeignKey(e => e.PostId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.PartnerUserId);
                entity.HasIndex(e => e.PostId);
                entity.HasIndex(e => e.CustomerUserId);
                entity.HasIndex(e => e.PaymentStatus);
            });

            // ========================
            // NOTIFICATION
            // ========================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                
                entity.Property(e => e.Type)
                      .IsRequired()
                      .HasMaxLength(50);
                
                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(200);
                
                entity.Property(e => e.Message)
                      .IsRequired()
                      .HasMaxLength(1000);
                
                entity.Property(e => e.Link)
                      .HasMaxLength(500);
                
                entity.Property(e => e.IsRead)
                      .HasDefaultValue(false);
                
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
            });

            // ========================
            // CHAT
            // ========================
            modelBuilder.Entity<ChatThread>(entity =>
            {
                entity.HasKey(e => e.ChatThreadId);

                entity.HasIndex(e => e.BookingId).IsUnique();
                entity.HasIndex(e => new { e.CustomerUserId, e.LastMessageAt });
                entity.HasIndex(e => new { e.PartnerUserId, e.LastMessageAt });

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.LastMessageAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CustomerUnreadCount).HasDefaultValue(0);
                entity.Property(e => e.PartnerUnreadCount).HasDefaultValue(0);

                // Ignore LastMessage - it's not a database column, only used for temporary storage
                entity.Ignore(e => e.LastMessage);
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.ChatMessageId);

                entity.Property(e => e.Content)
                      .IsRequired()
                      .HasMaxLength(2000);

                entity.Property(e => e.ImageUrl)
                      .HasMaxLength(500);

                entity.Property(e => e.MessageType)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue("Text");

                entity.Property(e => e.IsRead).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => new { e.ChatThreadId, e.CreatedAt });
                entity.HasIndex(e => new { e.ReceiverUserId, e.IsRead, e.CreatedAt });
            });

            // ========================
            // APPLY OTHER CONFIGS
            // ========================
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
