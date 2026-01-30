using System;
using System.Collections.Generic;

namespace TripCompass.WebUI.ViewModels
{
    public class AiChatRequest
    {
        public string Message { get; set; } = null!;
    }

    public class AiChatResponse
    {
        public string AssistantName { get; set; } = "TripCompass AI";
        public string Reply { get; set; } = null!;
        public AiItineraryResponse? Itinerary { get; set; }
        public List<string> QuickReplies { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

