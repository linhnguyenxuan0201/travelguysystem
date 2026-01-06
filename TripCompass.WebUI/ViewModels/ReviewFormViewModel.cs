using System.ComponentModel.DataAnnotations;
using TripCompass.Domain.Entities;
using TripCompass.Domain.Enums;
namespace TripCompass.WebUI.ViewModels;

public class ReviewFormViewModel
{
    public long PostId { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Content { get; set; } = null!;

    [Required]
    public string Location { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    public long SelectedCategoryId { get; set; }

    // 🔥 ẢNH MỚI
    public List<IFormFile>? Images { get; set; }

    // 🔥 ẢNH CŨ
    public List<ReviewImageVm> ExistingImages { get; set; } = new();

    // 🔥 ID ẢNH BỊ XOÁ
    public List<long> DeletedImageIds { get; set; } = new();

    public List<Category> AllCategories { get; set; } = new();
    public PostStatus Status { get; set; } = PostStatus.Pending; // ✅ FIX

}

public class ReviewImageVm
{
    public long PostImageId { get; set; }
    public long ImageId { get; set; }
    public string ImageUrl { get; set; } = null!;
}
