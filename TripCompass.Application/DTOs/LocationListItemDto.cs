namespace TripCompass.Application.DTOs
{
    public class LocationListItemDto
    {
        public string Location { get; set; } = null!;
        public int PostCount { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public DateTime? LastPostDate { get; set; }
    }
}
