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

            if (user == null || user.IsBanned)
                return null;

            if (!_hasher.Verify(user.PasswordHash, password))
                return null;

            return user;
        }
        public async Task<User> LoginWithGoogleAsync(string email, string name)
        {
            var user = await _userRepo.GetByEmailAsync(email);

            if (user != null)
                return user;

            return await _userRepo.CreateGoogleUserAsync(email, name);
        }

    }
}
