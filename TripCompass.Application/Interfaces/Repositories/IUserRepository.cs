using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        
        Task<User> CreateGoogleUserAsync(string email, string name);
        Task AddAsync(User user);
        Task AssignRoleAsync(User user, string roleName);
        Task<bool> EmailExistsAsync(string email);
    }
}
