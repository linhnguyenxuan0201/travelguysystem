using TripCompass.Domain.Entities;

namespace TripCompass.WebUI.ViewModels
{
    public class ReviewsPageViewModel
    {
        public string? SelectedCategory { get; set; }
        public bool? ShowPartnerOnly { get; set; }
        public string? SearchQuery { get; set; }
        public string? SelectedProvince { get; set; }
        public string? SelectedPriceRange { get; set; }
        public string? SortBy { get; set; }

        // Stats
        public int TotalProducts { get; set; }
        public double AverageRating { get; set; }

        // Featured posts (no category, high reputation)
        public List<ReviewItemViewModel> FeaturedPosts { get; set; } = new();

        // Partner posts (no category)
        public List<ReviewItemViewModel> PartnerPosts { get; set; } = new();

        // Category sections
        public List<CategorySectionViewModel> CategorySections { get; set; } = new();

        // All categories for filter
        public List<Category> Categories { get; set; } = new();

        // All provinces for filter
        public List<string> Provinces { get; set; } = new();
    }

    public class CategorySectionViewModel
    {
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string CategorySlug { get; set; } = null!;
        public List<ReviewItemViewModel> FeaturedPosts { get; set; } = new();
        public List<ReviewItemViewModel> PartnerPosts { get; set; } = new();
    }

    public class ReviewItemViewModel
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string? Location { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int Rating { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public decimal? Price { get; set; }
        public int ReputationImpact { get; set; }
        public bool IsPartner { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = null!;
        public string? AuthorAvatar { get; set; }
    }
}
