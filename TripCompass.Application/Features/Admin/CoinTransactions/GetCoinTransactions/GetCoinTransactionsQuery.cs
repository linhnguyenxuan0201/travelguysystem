using MediatR;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Features.Admin.CoinTransactions.GetCoinTransactions
{
    public class GetCoinTransactionsQuery : IRequest<PaginatedList<CoinTransactionListItemDto>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        
        public string? SearchTerm { get; set; }
        public string? Type { get; set; }
        public long? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
