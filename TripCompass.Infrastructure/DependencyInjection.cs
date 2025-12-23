using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripCompass.Application.Auth;
using TripCompass.Application.Common.Security;
using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Infrastructure.Auth;
using TripCompass.Infrastructure.Persistence;
using TripCompass.Infrastructure.Repositories;
using TripCompass.Infrastructure.Security;
using TripCompass.Infrastructure.Services;

namespace TripCompass.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config)
        {
            // ================= DB CONTEXT =================
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("DefaultConnection")));

            // ================= REPOSITORIES =================
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IUserAvatarRepository, UserAvatarRepository>();

            // 🔥 BẮT BUỘC – FIX LỖI 500 REVIEW
            services.AddScoped<IPostRepository, PostRepository>();

            // ================= UNIT OF WORK =================
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ================= SECURITY =================
            services.AddScoped<IPasswordHasher, EfPasswordHasher>();

            // ================= USER CONTEXT =================
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // (Giữ nguyên service cũ cho dropdown)
            services.AddScoped<UserContextService>();

            // ================= EMAIL =================
            services.AddScoped<IEmailService, SmtpEmailService>();

            return services;
        }
    }
}
