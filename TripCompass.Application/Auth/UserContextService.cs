using System;
using System.Collections.Generic;
using System.Text;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;

namespace TripCompass.Application.Auth
{
    

    public class UserContextService
    {
        private readonly IUserRepository _userRepo;
        private readonly IUnitOfWork _uow;

        public UserContextService(
            IUserRepository userRepo,
            IUnitOfWork uow)
        {
            _userRepo = userRepo;
            _uow = uow;
        }

        public async Task<UserDropdownDto?> GetDropdownAsync(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) return null;

            var wallet = await _uow.Wallets.GetByUserIdAsync(user.UserId);
            var avatar = await _uow.UserAvatars.GetActiveByUserIdAsync(user.UserId);

            return new UserDropdownDto
            {
                UserName = user.UserName,
                AvatarUrl = avatar?.AvatarUrl ?? "/images/avatar-default.png",
                ReputationLevel = user.ReputationLevel,
                ReputationScore = user.ReputationScore,
                WalletBalance = wallet?.Balance ?? 0
            };
        }

    }

}
