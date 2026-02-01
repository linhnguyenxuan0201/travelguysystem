namespace TripCompass.Application.DTOs
{
    public class CategoryListItemDto
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Icon { get; set; }
        public int PostCount { get; set; }
    }
}
