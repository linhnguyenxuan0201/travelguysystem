using System;
using System.Collections.Generic;

namespace TripCompass.WebUI.ViewModels
{
    public class AiItineraryRequest
    {
        public string Destination { get; set; } = null!;
        public int Days { get; set; } = 3;
        public int Nights { get; set; } = 2;
        public int BudgetVnd { get; set; } = 3000000;
        public string? Preferences { get; set; }
        public int People { get; set; } = 1;
    }

    public class AiSuggestionViewModel
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string? Location { get; set; }
        public decimal? Price { get; set; }
        public bool IsPartner { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Reason { get; set; }
    }

    public class AiItineraryDayViewModel
    {
        public int DayNumber { get; set; }
        public string Title { get; set; } = null!;
        public string Morning { get; set; } = null!;
        public string Afternoon { get; set; } = null!;
        public string Evening { get; set; } = null!;
        public string EstimatedCostNote { get; set; } = null!;
    }

    public class AiItineraryResponse
    {
        public string AssistantName { get; set; } = "TripCompass AI";
        public string Summary { get; set; } = null!;
        public List<string> FollowUpQuestions { get; set; } = new();
        public List<AiItineraryDayViewModel> Days { get; set; } = new();
        public List<AiSuggestionViewModel> Suggestions { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

