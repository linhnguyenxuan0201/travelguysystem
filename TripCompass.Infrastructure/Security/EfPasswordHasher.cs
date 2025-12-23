using Microsoft.AspNetCore.Identity;
using TripCompass.Application.Common.Security;

namespace TripCompass.Infrastructure.Security
{
    public class EfPasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<string> _hasher = new();

        public string Hash(string password)
            => _hasher.HashPassword(null!, password);

        public bool Verify(string hash, string password)
            => _hasher.VerifyHashedPassword(null!, hash, password)
               == PasswordVerificationResult.Success;
    }
}
