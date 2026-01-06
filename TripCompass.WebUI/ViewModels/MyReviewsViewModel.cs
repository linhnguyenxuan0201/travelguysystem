using TripCompass.Application.DTOs;
using TripCompass.Domain.Enums;

namespace TripCompass.WebUI.ViewModels
{
    public class MyReviewsViewModel
    {
        public PostStatus Status { get; set; }

        public List<ReviewListItemDto> Reviews { get; set; } = new();
        public int SelectedCategoryId { get; set; }

        public string? Keyword { get; set; }
        public long? CategoryId { get; set; }
        public int? Rating { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int FoodCount { get; set; }
        public int HotelCount { get; set; }
        public int EntertainmentCount
        {
            get; set;
        }
    }
}

