using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common.Security;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.WebUI.Controllers
{
    [AllowAnonymous]
    public class SetupController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUserRepository _userRepository;

        public SetupController(
            AppDbContext db,
            IPasswordHasher passwordHasher,
            IUserRepository userRepository)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            // Kiểm tra xem đã có admin với password hash thực sự chưa
            // (Bỏ qua admin với password hash placeholder từ seed data)
            var allUsers = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var adminUsers = allUsers.Where(u => 
                u.UserRoles != null && 
                u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName == "Admin"))
                .ToList();

            // Kiểm tra xem có admin với password hash thực sự không
            // Password hash thực sự từ ASP.NET Core Identity thường bắt đầu bằng "AQAAAA" và dài > 80 ký tự
            var hasValidAdmin = adminUsers.Any(u => 
                !string.IsNullOrEmpty(u.PasswordHash) &&
                u.PasswordHash != "HASH_ADMIN" &&
                u.PasswordHash != "HASH_MOD" &&
                u.PasswordHash != "HASH_USER1" &&
                u.PasswordHash != "HASH_USER2" &&
                u.PasswordHash != "GOOGLE" &&
                (u.PasswordHash.StartsWith("AQAAAA") || u.PasswordHash.Length > 80)); // Password hash thực sự

            if (hasValidAdmin)
            {
                ViewBag.Message = "Admin user already exists. Please login with existing admin account.";
                ViewBag.CanCreate = false;
            }
            else
            {
                ViewBag.CanCreate = true;
                // Kiểm tra xem có admin placeholder không
                var hasPlaceholderAdmin = adminUsers.Any(u => 
                    string.IsNullOrEmpty(u.PasswordHash) ||
                    u.PasswordHash == "HASH_ADMIN" ||
                    u.PasswordHash == "HASH_MOD" ||
                    u.PasswordHash == "HASH_USER1" ||
                    u.PasswordHash == "HASH_USER2" ||
                    u.PasswordHash == "GOOGLE" ||
                    (!u.PasswordHash.StartsWith("AQAAAA") && u.PasswordHash.Length <= 80));
                
                if (hasPlaceholderAdmin)
                {
                    ViewBag.Info = "Found placeholder admin account(s) from seed data. You can create a new admin account using the button below.";
                    ViewBag.HasPlaceholder = true;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(string userName, string email, string password)
        {
            // Kiểm tra lại xem đã có admin với password hash thực sự chưa
            // (Bỏ qua admin với password hash placeholder từ seed data)
            var allUsers = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var adminUsers = allUsers.Where(u => 
                u.UserRoles != null && 
                u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName == "Admin"))
                .ToList();

            var hasValidAdmin = adminUsers.Any(u => 
                !string.IsNullOrEmpty(u.PasswordHash) &&
                u.PasswordHash != "HASH_ADMIN" &&
                u.PasswordHash != "HASH_MOD" &&
                u.PasswordHash != "HASH_USER1" &&
                u.PasswordHash != "HASH_USER2" &&
                u.PasswordHash != "GOOGLE" &&
                (u.PasswordHash.StartsWith("AQAAAA") || u.PasswordHash.Length > 80)); // Password hash thực sự

            if (hasValidAdmin)
            {
                ModelState.AddModelError("", "Admin user with valid password already exists. Please login with existing admin account.");
                ViewBag.CanCreate = false;
                return View();
            }

            // Validation
            if (string.IsNullOrWhiteSpace(userName) || userName.Length < 3)
            {
                ModelState.AddModelError("userName", "Username must be at least 3 characters.");
                ViewBag.CanCreate = true;
                return View();
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                ModelState.AddModelError("email", "Invalid email address.");
                ViewBag.CanCreate = true;
                return View();
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("password", "Password must be at least 6 characters.");
                ViewBag.CanCreate = true;
                return View();
            }

            // Kiểm tra email/username đã tồn tại chưa
            if (await _userRepository.EmailExistsAsync(email))
            {
                ModelState.AddModelError("email", "Email already exists.");
                ViewBag.CanCreate = true;
                return View();
            }

            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (existingUser != null)
            {
                ModelState.AddModelError("userName", "Username already exists.");
                ViewBag.CanCreate = true;
                return View();
            }

            try
            {
                // Tạo user
                var passwordHash = _passwordHasher.Hash(password);
                var user = new User(userName, email, passwordHash);
                user.ReputationScore = 1000;
                user.ReputationLevel = 5;

                await _userRepository.AddAsync(user);
                await _userRepository.AssignRoleAsync(user, "Admin");

                // Tạo wallet (reload user để có UserId)
                user = await _userRepository.GetByEmailAsync(email);
                if (user != null)
                {
                    var wallet = new Wallet
                    {
                        UserId = user.UserId,
                        Balance = 1000,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Wallets.Add(wallet);
                    await _db.SaveChangesAsync();
                }

                ViewBag.Success = true;
                ViewBag.Message = $"Admin user '{userName}' created successfully! You can now login.";
                ViewBag.CanCreate = false;

                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating admin user: {ex.Message}");
                ViewBag.CanCreate = true;
                return View();
            }
        }

        /// <summary>
        /// Generate password hash - chỉ để lấy hash cho script SQL
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GenerateHash(string password = "Admin@123")
        {
            var hash = _passwordHasher.Hash(password);
            return Content($"Password: {password}\nHash: {hash}\n\nScript SQL:\n" +
                $"-- Tạo admin với password: {password}\n" +
                $"UPDATE Users SET PasswordHash = '{hash}' WHERE Email = 'admin@tripcompass.com' OR UserName = 'admin';\n" +
                $"IF @@ROWCOUNT = 0\n" +
                $"BEGIN\n" +
                $"  INSERT INTO Users (UserName, Email, PasswordHash, ReputationScore, ReputationLevel, IsBanned, CreatedAt)\n" +
                $"  VALUES ('admin', 'admin@tripcompass.com', '{hash}', 1000, 5, 0, GETUTCDATE());\n" +
                $"  DECLARE @UserId BIGINT = SCOPE_IDENTITY();\n" +
                $"  INSERT INTO UserRoles (UserId, RoleId) SELECT @UserId, RoleId FROM Roles WHERE RoleName = 'Admin';\n" +
                $"  INSERT INTO Wallets (UserId, Balance, UpdatedAt) VALUES (@UserId, 1000, GETUTCDATE());\n" +
                $"END", "text/plain");
        }

        /// <summary>
        /// Tự động tạo admin khi truy cập URL này (GET request - không cần form)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AutoCreateAdmin()
        {
            // Kiểm tra xem đã có admin với password hash thực sự chưa
            var allUsers = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var adminUsers = allUsers.Where(u => 
                u.UserRoles != null && 
                u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName == "Admin"))
                .ToList();

            var hasValidAdmin = adminUsers.Any(u => 
                !string.IsNullOrEmpty(u.PasswordHash) &&
                u.PasswordHash != "HASH_ADMIN" &&
                u.PasswordHash != "HASH_MOD" &&
                u.PasswordHash != "HASH_USER1" &&
                u.PasswordHash != "HASH_USER2" &&
                u.PasswordHash != "GOOGLE" &&
                (u.PasswordHash.StartsWith("AQAAAA") || u.PasswordHash.Length > 80));

            if (hasValidAdmin)
            {
                TempData["Message"] = "Admin user already exists. Redirecting to login...";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Thông tin admin mặc định
                var defaultUserName = "admin";
                var defaultEmail = "admin@tripcompass.com";
                var defaultPassword = "Admin@123";

                // Xóa admin placeholder cũ nếu có
                var existingUser = await _db.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == defaultEmail || u.UserName == defaultUserName);

                if (existingUser != null)
                {
                    // Xóa wallet nếu có
                    var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == existingUser.UserId);
                    if (wallet != null)
                    {
                        _db.Wallets.Remove(wallet);
                    }

                    // Xóa user roles
                    _db.UserRoles.RemoveRange(existingUser.UserRoles);
                    
                    // Xóa user
                    _db.Users.Remove(existingUser);
                    await _db.SaveChangesAsync();
                }

                // Tạo admin mới
                var passwordHash = _passwordHasher.Hash(defaultPassword);
                var user = new User(defaultUserName, defaultEmail, passwordHash);
                user.ReputationScore = 1000;
                user.ReputationLevel = 5;

                await _userRepository.AddAsync(user);
                await _userRepository.AssignRoleAsync(user, "Admin");

                // Tạo wallet
                user = await _userRepository.GetByEmailAsync(defaultEmail);
                if (user != null)
                {
                    var wallet = new Wallet
                    {
                        UserId = user.UserId,
                        Balance = 1000,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Wallets.Add(wallet);
                    await _db.SaveChangesAsync();
                }

                TempData["Success"] = $"Admin created successfully!";
                TempData["AdminEmail"] = defaultEmail;
                TempData["AdminPassword"] = defaultPassword;
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating admin: {ex.Message}";
                return RedirectToAction("Login", "Account");
            }
        }

        /// <summary>
        /// Tự động tạo admin với thông tin mặc định (chỉ dùng cho setup lần đầu)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDefaultAdmin()
        {
            // Kiểm tra xem đã có admin với password hash thực sự chưa
            var allUsers = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var adminUsers = allUsers.Where(u => 
                u.UserRoles != null && 
                u.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName == "Admin"))
                .ToList();

            var hasValidAdmin = adminUsers.Any(u => 
                !string.IsNullOrEmpty(u.PasswordHash) &&
                u.PasswordHash != "HASH_ADMIN" &&
                u.PasswordHash != "HASH_MOD" &&
                u.PasswordHash != "HASH_USER1" &&
                u.PasswordHash != "HASH_USER2" &&
                u.PasswordHash != "GOOGLE" &&
                (u.PasswordHash.StartsWith("AQAAAA") || u.PasswordHash.Length > 80)); // Password hash thực sự

            if (hasValidAdmin)
            {
                ViewBag.Message = "Admin user already exists. Please login with existing admin account.";
                ViewBag.CanCreate = false;
                return View("CreateAdmin");
            }

            try
            {
                // Thông tin admin mặc định
                var defaultUserName = "admin";
                var defaultEmail = "admin@tripcompass.com";
                var defaultPassword = "Admin@123"; // Password mặc định - nên đổi sau khi đăng nhập

                // Kiểm tra email/username đã tồn tại chưa
                if (await _userRepository.EmailExistsAsync(defaultEmail))
                {
                    // Nếu đã tồn tại nhưng là placeholder, xóa và tạo lại
                    var existingUser = await _db.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.Email == defaultEmail);

                    if (existingUser != null)
                    {
                        // Xóa wallet nếu có
                        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == existingUser.UserId);
                        if (wallet != null)
                        {
                            _db.Wallets.Remove(wallet);
                        }

                        // Xóa user roles
                        _db.UserRoles.RemoveRange(existingUser.UserRoles);
                        
                        // Xóa user
                        _db.Users.Remove(existingUser);
                        await _db.SaveChangesAsync();
                    }
                }

                // Tạo admin mới
                var passwordHash = _passwordHasher.Hash(defaultPassword);
                var user = new User(defaultUserName, defaultEmail, passwordHash);
                user.ReputationScore = 1000;
                user.ReputationLevel = 5;

                await _userRepository.AddAsync(user);
                await _userRepository.AssignRoleAsync(user, "Admin");

                // Tạo wallet
                user = await _userRepository.GetByEmailAsync(defaultEmail);
                if (user != null)
                {
                    var wallet = new Wallet
                    {
                        UserId = user.UserId,
                        Balance = 1000,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Wallets.Add(wallet);
                    await _db.SaveChangesAsync();
                }

                ViewBag.Success = true;
                ViewBag.Message = $"Admin account created successfully!<br><br><strong>Thông tin đăng nhập:</strong><br>Email: <code>{defaultEmail}</code><br>Password: <code>{defaultPassword}</code><br><br><small class='text-warning'>⚠️ Vui lòng đổi mật khẩu sau khi đăng nhập!</small>";
                ViewBag.CanCreate = false;
                ViewBag.DefaultEmail = defaultEmail;
                ViewBag.DefaultPassword = defaultPassword;
                
                return View("CreateAdmin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating admin user: {ex.Message}");
                ViewBag.CanCreate = true;
                return View("CreateAdmin");
            }
        }
    }
}
