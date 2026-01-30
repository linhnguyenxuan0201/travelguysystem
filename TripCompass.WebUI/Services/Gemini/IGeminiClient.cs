using System.Threading;
using System.Threading.Tasks;

namespace TripCompass.WebUI.Services.Gemini
{
    public interface IGeminiClient
    {
        Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default);
        bool IsConfigured();
    }
}

