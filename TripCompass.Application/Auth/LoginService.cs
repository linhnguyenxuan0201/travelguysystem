using Microsoft.Extensions.Options;
using TripCompass.Application.Common.Security;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Auth
{
    public class LoginService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher _hasher;
        private readonly AdminConfig _adminConfig;

        public LoginService(
            IUserRepository userRepo, 
            IPasswordHasher hasher,
            IOptions<AdminConfig> adminConfig)
        {
            _userRepo = userRepo;
            _hasher = hasher;
            _adminConfig = adminConfig.Value;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            // Check admin from config first (không lấy từ database)
            if (_adminConfig.Enabled && 
                !string.IsNullOrEmpty(_adminConfig.Email) && 
                !string.IsNullOrEmpty(_adminConfig.Password))
            {
                if (email.Equals(_adminConfig.Email, StringComparison.OrdinalIgnoreCase) &&
                    password == _adminConfig.Password)
                {
                    // Return virtual admin user (không lưu trong database)
                    // Sử dụng PasswordHash = "CONFIG_ADMIN" để identify admin từ config
                    var configAdmin = new User(_adminConfig.UserName, _adminConfig.Email, "CONFIG_ADMIN");
                    // Set properties có thể set được
                    configAdmin.ReputationScore = 1000;
                    configAdmin.ReputationLevel = 5;
                    configAdmin.CreatedAt = DateTime.UtcNow;
                    return configAdmin;
                }
            }

            // Check database users (bỏ qua admin từ database nếu có config admin)
            if (_adminConfig.Enabled && 
                !string.IsNullOrEmpty(_adminConfig.Email) &&
                email.Equals(_adminConfig.Email, StringComparison.OrdinalIgnoreCase))
            {
                // Nếu là email admin nhưng password không đúng config, reject
                return null;
            }

            var user = await _userRepo.GetByEmailAsync(email);

            if (user == null)
                return null;

            // Reject placeholder / non-password accounts
            if (string.IsNullOrEmpty(user.PasswordHash) ||
                user.PasswordHash == "HASH_ADMIN" ||
                user.PasswordHash == "HASH_MOD" ||
                user.PasswordHash == "HASH_USER1" ||
                user.PasswordHash == "HASH_USER2" ||
                user.PasswordHash == "GOOGLE")
            {
                return null;
            }

            try
            {
                if (!_hasher.Verify(user.PasswordHash, password))
                    return null;
            }
            catch (System.FormatException)
            {
                // Corrupted/legacy hash in database (ex: seeded as plain text)
                return null;
            }

            return user;
        }
        public async Task<User> LoginWithGoogleAsync(string email, string name)
        {
            var user = await _userRepo.GetByEmailAsync(email);

            if (user != null)
                return user;

            // Create new Google user (this will assign "User" role)
            user = await _userRepo.CreateGoogleUserAsync(email, name);
            
            // Reload user with UserRoles to ensure they're loaded
            return await _userRepo.GetByEmailAsync(email) ?? user;
        }

    }
}
