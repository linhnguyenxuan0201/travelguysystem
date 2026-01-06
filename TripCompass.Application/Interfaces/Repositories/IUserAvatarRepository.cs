using System;
using System.Collections.Generic;
using System.Text;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface IUserAvatarRepository
    {
        Task<UserAvatar?> GetActiveByUserIdAsync(long userId);
        Task AddAsync(UserAvatar avatar);
        Task DeactivateAllAsync(long userId);
    }
}
