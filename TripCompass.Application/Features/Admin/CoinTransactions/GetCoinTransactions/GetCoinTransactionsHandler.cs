using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.CoinTransactions.GetCoinTransactions
{
    public class GetCoinTransactionsHandler : IRequestHandler<GetCoinTransactionsQuery, PaginatedList<CoinTransactionListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCoinTransactionsHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<CoinTransactionListItemDto>> Handle(GetCoinTransactionsQuery request, CancellationToken cancellationToken)
        {
            var query = from transaction in _context.CoinTransactions
                       join user in _context.Users on transaction.UserId equals user.UserId
                       select new { transaction, user };

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(x =>
                    x.transaction.TransactionId.ToString() == term ||
                    x.user.UserName.ToLower().Contains(term) ||
                    x.user.Email.ToLower().Contains(term) ||
                    x.transaction.Type.ToLower().Contains(term));
            }

            // Filter by Type
            if (!string.IsNullOrEmpty(request.Type))
            {
                query = query.Where(x => x.transaction.Type == request.Type);
            }

            // Filter by UserId
            if (request.UserId.HasValue)
            {
                query = query.Where(x => x.transaction.UserId == request.UserId.Value);
            }

            // Filter by Date
            if (request.FromDate.HasValue)
            {
                query = query.Where(x => x.transaction.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x => x.transaction.CreatedAt <= request.ToDate.Value);
            }

            // Order By (Default: Newest first)
            query = query.OrderByDescending(x => x.transaction.CreatedAt);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Get transactions with pagination
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => x.transaction)
                .ToListAsync(cancellationToken);

            // Get user info
            var userIds = items.Select(t => t.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.UserName, u.Email })
                .ToListAsync(cancellationToken);

            // Project to DTOs
            var transactionDtos = items.Select(t =>
            {
                var user = users.FirstOrDefault(u => u.UserId == t.UserId);
                
                return new CoinTransactionListItemDto
                {
                    TransactionId = t.TransactionId,
                    UserId = t.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    UserEmail = user?.Email ?? "Unknown",
                    Amount = t.Amount,
                    Type = t.Type,
                    TypeDisplay = GetTypeDisplayName(t.Type),
                    ReferenceId = t.ReferenceId,
                    CreatedAt = t.CreatedAt
                };
            }).ToList();

            return new PaginatedList<CoinTransactionListItemDto>(
                transactionDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);
        }

        private string GetTypeDisplayName(string type)
        {
            return type switch
            {
                "EARNED" => "Kiếm được",
                "SPENT" => "Đã chi",
                "PURCHASED" => "Mua",
                "REFUND" => "Hoàn tiền",
                "BONUS" => "Thưởng",
                _ => type
            };
        }
    }
}
