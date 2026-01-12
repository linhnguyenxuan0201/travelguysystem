using Microsoft.EntityFrameworkCore;
using TripCompass.Domain.Entities;
using TripCompass.Domain.Enums;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // 1. Roles
            if (!await context.Roles.AnyAsync())
            {
                context.Roles.AddRange(
                    new Role { RoleName = "Admin" },
                    new Role { RoleName = "User" },
                    new Role { RoleName = "Moderator" }
                );
                await context.SaveChangesAsync();
            }

            // 2. Users
            if (!await context.Users.AnyAsync())
            {
                var users = new List<User>();
                var passwordHash = "HASHED_PASSWORD"; // In real app use hasher

                // Admin
                users.Add(new User("admin", "admin@tripcompass.com", passwordHash) { CreatedAt = DateTime.UtcNow.AddMonths(-6) });
                
                // Regular Users (Active)
                for (int i = 1; i <= 20; i++)
                {
                    var date = DateTime.UtcNow.AddDays(-new Random().Next(0, 30));
                    users.Add(new User($"user{i}", $"user{i}@example.com", passwordHash) { CreatedAt = date, ReputationScore = new Random().Next(0, 500) });
                }

                // Banned Users
                for (int i = 1; i <= 5; i++)
                {
                    var user = new User($"banned{i}", $"banned{i}@example.com", passwordHash) { CreatedAt = DateTime.UtcNow.AddMonths(-1) };
                    user.Ban();
                    users.Add(user);
                }

                context.Users.AddRange(users);
                await context.SaveChangesAsync();

                // Assign Roles
                var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin");
                var userRole = await context.Roles.FirstAsync(r => r.RoleName == "User");
                
                var adminUser = await context.Users.FirstAsync(u => u.UserName == "admin");
                context.UserRoles.Add(new UserRole { UserId = adminUser.UserId, RoleId = adminRole.RoleId });

                foreach (var u in users.Where(x => x.UserName.StartsWith("user") || x.UserName.StartsWith("banned")))
                {
                    context.UserRoles.Add(new UserRole { UserId = u.UserId, RoleId = userRole.RoleId });
                }
                await context.SaveChangesAsync();
            }

            // 3. Wallets
            if (!await context.Wallets.AnyAsync())
            {
                var userIds = await context.Users.Select(u => u.UserId).ToListAsync();
                var wallets = userIds.Select(id => new Wallet
                {
                    UserId = id,
                    Balance = new Random().Next(0, 1000),
                    UpdatedAt = DateTime.UtcNow
                });
                context.Wallets.AddRange(wallets);
                await context.SaveChangesAsync();
            }

            // 4. Posts
            if (!await context.Posts.AnyAsync())
            {
                var users = await context.Users.Where(u => !u.IsBanned).ToListAsync();
                var posts = new List<Post>();

                foreach (var user in users)
                {
                    // Active Posts
                    for (int i = 0; i < new Random().Next(1, 4); i++)
                    {
                        posts.Add(new Post
                        {
                            UserId = user.UserId,
                            Title = $"Trip to Location {i} by {user.UserName}",
                            Content = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                            Location = "Vietnam",
                            ViewCount = new Random().Next(10, 5000),
                            CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(1, 60)),
                            Status = PostStatus.Published
                        });
                    }

                    // Pending Posts
                    if (new Random().Next(0, 2) == 1)
                    {
                        posts.Add(new Post
                        {
                            UserId = user.UserId,
                            Title = $"Pending Post by {user.UserName}",
                            Content = "Waiting for approval...",
                            Location = "Da Nang",
                            CreatedAt = DateTime.UtcNow,
                            Status = PostStatus.Pending
                        });
                    }
                }
                context.Posts.AddRange(posts);
                await context.SaveChangesAsync();
            }

            // 5. Reports
            if (!await context.Reports.AnyAsync())
            {
                var users = await context.Users.ToListAsync();
                var posts = await context.Posts.ToListAsync();
                
                if (users.Any() && posts.Any())
                {
                    var reports = new List<Report>();
                    for (int i = 0; i < 10; i++)
                    {
                        reports.Add(new Report
                        {
                            ReporterId = users[new Random().Next(users.Count)].UserId,
                            TargetType = "Post",
                            TargetId = posts[new Random().Next(posts.Count)].PostId,
                            Reason = "Inappropriate content",
                            Status = 0, // Pending
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    context.Reports.AddRange(reports);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
