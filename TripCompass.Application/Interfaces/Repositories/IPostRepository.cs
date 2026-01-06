using System;
using System.Collections.Generic;
using System.Text;
using TripCompass.Application.DTOs;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface IPostRepository
    {
        Task<(List<ReviewListItemDto> Items, int TotalCount)>
            GetUserReviewsAsync(
                long userId,
                string? keyword,
                long? categoryId,
                int? rating,
                int page,
                int pageSize);
        Task<List<MonthlyStatDto>> GetMonthlyStatsAsync(long userId, int year);

        Task<List<HeatmapDto>> GetHeatmapAsync(long userId, int year);
    }
}
