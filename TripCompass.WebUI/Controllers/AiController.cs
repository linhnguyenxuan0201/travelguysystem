using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using TripCompass.Domain.Enums;
using TripCompass.Infrastructure.Persistence;
using TripCompass.WebUI.ViewModels;
using TripCompass.WebUI.Services.Gemini;

namespace TripCompass.WebUI.Controllers
{
    public class AiController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IGeminiClient _gemini;

        public AiController(AppDbContext db, IGeminiClient gemini)
        {
            _db = db;
            _gemini = gemini;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Demo()
        {
            // Default example
            var vm = new AiItineraryRequest
            {
                Destination = "Đà Nẵng",
                Days = 3,
                Nights = 2,
                BudgetVnd = 3000000,
                People = 1,
                Preferences = "biển, ăn hải sản, chụp ảnh"
            };
            return View(vm);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/ai/itinerary")]
        public async Task<IActionResult> GenerateItinerary([FromBody] AiItineraryRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Destination))
            {
                return BadRequest(new { error = "Destination is required" });
            }

            req.Days = Math.Clamp(req.Days, 1, 10);
            req.Nights = Math.Clamp(req.Nights, 0, 9);
            req.People = Math.Clamp(req.People, 1, 10);
            req.BudgetVnd = Math.Clamp(req.BudgetVnd, 100000, 100000000);

            // Basic follow-up questions if preferences are missing
            var resp = new AiItineraryResponse
            {
                Summary = $"Gợi ý lịch trình {req.Days} ngày {req.Nights} đêm ở {req.Destination} với ngân sách {req.BudgetVnd:N0}₫.",
            };

            if (string.IsNullOrWhiteSpace(req.Preferences))
            {
                resp.FollowUpQuestions.Add("Bạn đi mấy người và ưu tiên thiên về biển, núi hay phố?");
                resp.FollowUpQuestions.Add("Bạn có muốn ở gần biển (Mỹ Khê) hay gần trung tâm?");
                resp.FollowUpQuestions.Add("Bạn có phương tiện di chuyển (xe máy/ô tô) hay cần gợi ý thuê xe?");
            }

            // Query posts from TripCompass DB
            var baseQuery = _db.Posts
                .AsNoTracking()
                .Include(p => p.PostImages)
                .Include(p => p.PostCategories).ThenInclude(pc => pc.Category)
                .Where(p => !p.IsDeleted && p.Status == PostStatus.Published);

            // Destination match (contains)
            var dest = req.Destination.Trim();
            baseQuery = baseQuery.Where(p => p.Location != null && p.Location.Contains(dest));

            // Heuristic: try to pick "hotel" and "food" by category slug/name keywords
            var suggestionsQuery = baseQuery
                .OrderByDescending(p => p.IsPartner)
                .ThenByDescending(p => p.ReputationImpact)
                .ThenByDescending(p => p.ViewCount)
                .Take(12);

            var posts = await suggestionsQuery.ToListAsync();

            resp.Suggestions = posts.Select(p =>
            {
                var thumb = p.PostImages.OrderByDescending(x => x.PostImageId).FirstOrDefault()?.ImageUrl;
                var catText = string.Join(", ", p.PostCategories.Select(pc => pc.Category.Slug ?? pc.Category.Name).Take(2));
                return new AiSuggestionViewModel
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Location = p.Location,
                    Price = p.Price,
                    IsPartner = p.IsPartner,
                    ThumbnailUrl = thumb,
                    Reason = string.IsNullOrWhiteSpace(catText) ? "Phù hợp với điểm đến" : $"Thuộc nhóm: {catText}"
                };
            }).ToList();

            // Simple itinerary template (can be improved later)
            for (int d = 1; d <= req.Days; d++)
            {
                var title = d == 1 ? "Check-in & khám phá nhẹ nhàng" :
                            d == req.Days ? "Thư giãn & mua quà" : "Trải nghiệm điểm nhấn";

                resp.Days.Add(new AiItineraryDayViewModel
                {
                    DayNumber = d,
                    Title = $"Ngày {d}: {title}",
                    Morning = d == 1
                        ? "Đến nơi, nhận phòng (nếu được). Ăn sáng nhẹ, cà phê và dạo biển."
                        : "Ăn sáng. Tham quan 1 điểm nổi bật gần trung tâm/biển.",
                    Afternoon = d == 2
                        ? "Khám phá khu vui chơi/điểm ngắm cảnh. Ăn trưa theo ngân sách."
                        : "Ăn trưa. Nghỉ ngơi. Chiều đi chụp ảnh/điểm check-in.",
                    Evening = d == req.Days
                        ? "Ăn tối nhẹ. Mua quà. Chuẩn bị về."
                        : "Ăn tối (hải sản/đặc sản). Dạo phố đêm, chợ đêm nếu thích.",
                    EstimatedCostNote = $"Gợi ý chi tiêu: ~{(req.BudgetVnd / req.Days):N0}₫/ngày (ước tính, tuỳ số người & nơi ở)."
                });
            }

            return Ok(resp);
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/ai/chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var text = req.Message.Trim();
            var resp = new AiChatResponse();

            // Very small "NLU" for demo:
            // Extract destination: look for "ở <place>" or common cities
            string? destination = null;
            var mDest = Regex.Match(text, @"\b(?:ở|tai|tại)\s+([A-Za-zÀ-ỹ0-9\s\-]+)", RegexOptions.IgnoreCase);
            if (mDest.Success)
            {
                destination = mDest.Groups[1].Value.Trim();
            }
            else
            {
                // fallback common VN cities
                var known = new[] { "Đà Nẵng", "Da Nang", "Hà Nội", "Ha Noi", "Hồ Chí Minh", "Ho Chi Minh", "Đà Lạt", "Da Lat", "Huế", "Hue", "Phú Quốc", "Phu Quoc", "Nha Trang" };
                destination = known.FirstOrDefault(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));
            }

            // Days/Nights
            int? days = null;
            int? nights = null;
            var mDays = Regex.Match(text, @"(\d+)\s*(?:ngày|day)", RegexOptions.IgnoreCase);
            if (mDays.Success && int.TryParse(mDays.Groups[1].Value, out var d)) days = d;
            var mNights = Regex.Match(text, @"(\d+)\s*(?:đêm|night)", RegexOptions.IgnoreCase);
            if (mNights.Success && int.TryParse(mNights.Groups[1].Value, out var n)) nights = n;

            // Budget: "3 triệu", "3000000", "3tr"
            int? budget = null;
            var mTrieu = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*(?:triệu|tr)\b", RegexOptions.IgnoreCase);
            if (mTrieu.Success)
            {
                var raw = mTrieu.Groups[1].Value.Replace(",", ".").Trim();
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mil))
                {
                    budget = (int)Math.Round(mil * 1_000_000m, MidpointRounding.AwayFromZero);
                }
            }
            else
            {
                var mNum = Regex.Match(text, @"\b(\d{6,9})\b");
                if (mNum.Success && int.TryParse(mNum.Groups[1].Value, out var v)) budget = v;
            }

            // If user asks for itinerary (keywords) or provides days/budget => generate itinerary (our tool)
            var wantsItinerary =
                text.Contains("lịch trình", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("gợi ý", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("ở đâu", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("ăn gì", StringComparison.OrdinalIgnoreCase) ||
                days.HasValue || budget.HasValue;

            if (wantsItinerary)
            {
                if (string.IsNullOrWhiteSpace(destination))
                {
                    resp.Reply = "Bạn muốn đi đâu ạ? (vd: Đà Nẵng / Đà Lạt / Hà Nội…)";
                    resp.QuickReplies.AddRange(new[] { "3 ngày 2 đêm ở Đà Nẵng 3 triệu", "2 ngày 1 đêm ở Đà Lạt 2 triệu" });
                    return Ok(resp);
                }

                var itReq = new AiItineraryRequest
                {
                    Destination = destination!,
                    Days = days ?? 3,
                    Nights = nights ?? Math.Max(0, (days ?? 3) - 1),
                    BudgetVnd = budget ?? 3000000,
                    Preferences = text, // keep original text as preferences hint
                    People = 1
                };

                // reuse itinerary generator
                var itineraryResult = await GenerateItinerary(itReq) as OkObjectResult;
                var itinerary = itineraryResult?.Value as AiItineraryResponse;
                resp.Itinerary = itinerary;

                // Gemini for natural-language reply (optional)
                if (_gemini.IsConfigured())
                {
                    var suggestionsText = itinerary?.Suggestions?.Any() == true
                        ? string.Join("\n", itinerary!.Suggestions.Take(6).Select(s => $"- {s.Title} ({s.Location}) /Review/Detail/{s.PostId}"))
                        : "(Không có gợi ý trong DB theo Location.)";

                    var prompt =
                        "Bạn là TripCompass AI (trợ lý du lịch). Trả lời tiếng Việt, ngắn gọn, có cấu trúc.\n" +
                        $"Yêu cầu của khách: {text}\n\n" +
                        $"Thông tin hệ thống đã tìm trong TripCompass (một số gợi ý):\n{suggestionsText}\n\n" +
                        "Hãy:\n" +
                        "- Tóm tắt lịch trình 3-5 dòng (không cần quá chi tiết vì lịch trình chi tiết đã có).\n" +
                        "- Hỏi 2 câu để cá nhân hóa thêm (ví dụ: đi mấy người, ở gần biển hay trung tâm, có xe không).\n" +
                        "- Gợi ý 3 click tiếp theo (khách sạn/quán ăn/địa điểm) dựa trên gợi ý trong hệ thống.\n";

                    try
                    {
                        resp.Reply = await _gemini.GenerateTextAsync(prompt);
                    }
                    catch
                    {
                        // fallback
                        resp.Reply =
                            $"Mình gợi ý lịch trình {itReq.Days} ngày {itReq.Nights} đêm ở {itReq.Destination} với ngân sách {itReq.BudgetVnd:N0}₫.\n" +
                            $"Mình cũng đã tìm các bài viết liên quan trong TripCompass để bạn bấm xem chi tiết.";
                    }
                }
                else
                {
                    resp.Reply =
                        $"Mình gợi ý lịch trình {itReq.Days} ngày {itReq.Nights} đêm ở {itReq.Destination} với ngân sách {itReq.BudgetVnd:N0}₫.\n" +
                        $"Mình cũng đã tìm các bài viết liên quan trong TripCompass để bạn bấm xem chi tiết.";
                }

                resp.QuickReplies.AddRange(new[]
                {
                    $"Gợi ý khách sạn ở {itReq.Destination}",
                    $"Gợi ý quán ăn ở {itReq.Destination}",
                    $"Sửa ngân sách {itReq.BudgetVnd + 1000000:N0}"
                });

                return Ok(resp);
            }

            // Otherwise: website Q&A (Gemini if configured, else demo)
            if (text.Contains("premium", StringComparison.OrdinalIgnoreCase) || text.Contains("gói", StringComparison.OrdinalIgnoreCase))
            {
                if (_gemini.IsConfigured())
                {
                    try
                    {
                        resp.Reply = await _gemini.GenerateTextAsync(
                            "Bạn là TripCompass AI. Trả lời tiếng Việt.\n" +
                            $"Khách hỏi: {text}\n" +
                            "Nếu liên quan Premium, trả lời ngắn gọn: Premium có gì, cách nâng cấp, và gợi ý vào trang /Home/Premium.");
                    }
                    catch
                    {
                        resp.Reply = "Premium giúp bạn mở khóa nhiều tính năng nâng cao. Bạn có thể xem chi tiết tại trang Premium và nâng cấp trực tiếp.";
                    }
                }
                else
                {
                    resp.Reply = "Premium giúp bạn mở khóa nhiều tính năng nâng cao. Bạn có thể xem chi tiết tại trang Premium và nâng cấp trực tiếp.";
                }
                resp.QuickReplies.AddRange(new[] { "Xem trang Premium", "Nâng cấp Pro", "Nâng cấp Enterprise" });
                return Ok(resp);
            }

            if (_gemini.IsConfigured())
            {
                try
                {
                    resp.Reply = await _gemini.GenerateTextAsync(
                        "Bạn là TripCompass AI (trợ lý du lịch). Trả lời tiếng Việt.\n" +
                        $"Khách nói: {text}\n" +
                        "Hãy hỏi lại 1 câu để làm rõ mục tiêu và đưa 3 gợi ý prompt mẫu về lịch trình/khách sạn/quán ăn.");
                }
                catch
                {
                    resp.Reply = "Mình có thể giúp bạn gợi ý lịch trình (ví dụ: “3 ngày 2 đêm ở Đà Nẵng 3 triệu”) hoặc tìm địa điểm/khách sạn/quán ăn trong TripCompass.";
                }
            }
            else
            {
                resp.Reply = "Mình có thể giúp bạn gợi ý lịch trình (ví dụ: “3 ngày 2 đêm ở Đà Nẵng 3 triệu”) hoặc tìm địa điểm/khách sạn/quán ăn trong TripCompass.";
            }
            resp.QuickReplies.AddRange(new[] { "3 ngày 2 đêm ở Đà Nẵng 3 triệu", "Tìm khách sạn ở Đà Nẵng", "Tìm quán ăn ở Đà Nẵng" });
            return Ok(resp);
        }
    }
}

