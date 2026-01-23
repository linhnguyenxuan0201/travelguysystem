using TripCompass.Application.Common.Security;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Auth
{
    public class LoginService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPasswordHasher _hasher;

        public LoginService(IUserRepository userRepo, IPasswordHasher hasher)
        {
            _userRepo = userRepo;
            _hasher = hasher;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
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
