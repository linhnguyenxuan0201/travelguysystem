using System;
using System.Collections.Generic;
using System.Text;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByUserIdAsync(long userId);
    }
}
