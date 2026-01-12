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
            
            // TODO: Tạm thời comment để tránh lỗi khi bảng UserAvatars chưa tồn tại
            // var avatar = await _uow.UserAvatars.GetActiveByUserIdAsync(user.UserId);
            string? avatarUrl = null;
            try
            {
                var avatar = await _uow.UserAvatars.GetActiveByUserIdAsync(user.UserId);
                avatarUrl = avatar?.AvatarUrl;
            }
            catch
            {
                // Bảng UserAvatars chưa tồn tại, sử dụng avatar mặc định
                avatarUrl = null;
            }

            return new UserDropdownDto
            {
                UserName = user.UserName,
                AvatarUrl = avatarUrl ?? "/images/avatar-default.png",
                ReputationLevel = user.ReputationLevel,
                ReputationScore = user.ReputationScore,
                WalletBalance = wallet?.Balance ?? 0
            };
        }

    }

}
