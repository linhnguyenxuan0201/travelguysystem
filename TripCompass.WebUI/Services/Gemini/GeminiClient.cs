using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TripCompass.WebUI.Services.Gemini
{
    // Minimal REST client for Google Gemini (Generative Language API)
    // Docs: https://ai.google.dev/gemini-api/docs
    public class GeminiClient : IGeminiClient
    {
        private readonly HttpClient _http;
        private readonly GeminiOptions _opt;

        public GeminiClient(HttpClient http, IOptions<GeminiOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        public bool IsConfigured() => !string.IsNullOrWhiteSpace(_opt.ApiKey);

        public async Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default)
        {
            if (!IsConfigured())
            {
                throw new InvalidOperationException("Gemini ApiKey is not configured.");
            }

            var model = string.IsNullOrWhiteSpace(_opt.Model) ? "gemini-2.0-flash" : _opt.Model.Trim();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_opt.ApiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.6,
                    topP = 0.9,
                    maxOutputTokens = 600
                }
            };

            using var res = await _http.PostAsJsonAsync(url, payload, ct);
            var json = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Gemini API error {(int)res.StatusCode}: {json}");
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // candidates[0].content.parts[0].text
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var cand0 = candidates[0];
                if (cand0.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textEl))
                {
                    return textEl.GetString() ?? "";
                }
            }

            return "";
        }
    }
}

