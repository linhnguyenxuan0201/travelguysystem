using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;   // ✅ FIX Ở ĐÂY
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.Infrastructure.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AppDbContext _context;

        public WalletRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wallet?> GetByUserIdAsync(long userId)
        {
            return await _context.Wallets
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
