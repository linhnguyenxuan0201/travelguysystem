using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Domain.Enums;
using TripCompass.Infrastructure.Persistence;
using TripCompass.WebUI.ViewModels;

namespace TripCompass.WebUI.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IPostRepository _postRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly AppDbContext _context;

        public ReviewController(
            IPostRepository postRepository,
            ICurrentUserService currentUser,
            AppDbContext context)
        {
            _postRepository = postRepository;
            _currentUser = currentUser;
            _context = context;
        }

        // =========================
        // MY REVIEWS
        // =========================
        public async Task<IActionResult> MyReviews(
            string? keyword,
            long? categoryId,
            int? rating,
            int page = 1)
        {
            const int pageSize = 5;
            var userId = _currentUser.UserId;

            // ---------- CATEGORY FILTER ----------
            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            // ---------- LIST + FILTER ----------
            var (items, filteredCount) =
                await _postRepository.GetUserReviewsAsync(
                    userId,
                    keyword,
                    categoryId,
                    rating,
                    page,
                    pageSize);

            // ---------- TOTAL REVIEWS (NOT DELETED) ----------
            var totalAll = await _context.Posts
                .AsNoTracking()
                .CountAsync(p => p.UserId == userId && !p.IsDeleted);

            // ---------- STATS PER CATEGORY ----------
            var categoryStats = await _context.PostCategories
                .AsNoTracking()
                .Where(pc =>
                    pc.Post.UserId == userId &&
                    !pc.Post.IsDeleted)
                .GroupBy(pc => pc.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            int foodCount =
                categoryStats.FirstOrDefault(x => x.Category == "Food")?.Count ?? 0;

            int hotelCount =
                categoryStats.FirstOrDefault(x => x.Category == "Hotel")?.Count ?? 0;

            int entertainmentCount =
                categoryStats.FirstOrDefault(x => x.Category == "Entertainment")?.Count ?? 0;

            // ---------- VIEW MODEL ----------
            var vm = new MyReviewsViewModel
            {
                Reviews = items,

                Keyword = keyword,
                CategoryId = categoryId,
                Rating = rating,

                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(filteredCount / (double)pageSize),

                TotalCount = totalAll,
                FoodCount = foodCount,
                HotelCount = hotelCount,
                EntertainmentCount = entertainmentCount
            };

            return View(vm);
        }

        // =========================
        // CREATE (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ReviewFormViewModel
            {
                Status = PostStatus.Pending,// ✅ RẤT QUAN TRỌNG
                AllCategories = await _context.Categories.ToListAsync()
            };

            return View(model);
        }


        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllCategories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(model);
            }

            var userId = _currentUser.UserId;

            var post = new Domain.Entities.Post
            {
                UserId = userId,
                Title = model.Title,
                Content = model.Content,
                Location = model.Location,
                ReputationImpact = model.Rating,
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0,
                LikeCount = 0,
                IsDeleted = false,
                Status = model.Status
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // CATEGORY
            _context.PostCategories.Add(new Domain.Entities.PostCategory
            {
                PostId = post.PostId,
                CategoryId = model.SelectedCategoryId
            });

            // IMAGE UPLOAD
            if (model.Images != null && model.Images.Any())
            {
                var uploadDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/reviews");

                Directory.CreateDirectory(uploadDir);

                int sortOrder = 0;

                foreach (var file in model.Images)
                {
                    if (file.Length == 0) continue;

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var path = Path.Combine(uploadDir, fileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.PostImages.Add(new Domain.Entities.PostImage
                    {
                        PostId = post.PostId,
                        ImageUrl = $"/images/reviews/{fileName}",
                        IsCover = sortOrder == 0,
                        SortOrder = sortOrder,
                        CreatedAt = DateTime.UtcNow
                    });

                    sortOrder++;
                }
            }

            // ---------- REPUTATION ----------
            var user = await _context.Users.FirstAsync(u => u.UserId == userId);
            int earnedScore = new Random().Next(50, 201);

            user.ReputationScore += earnedScore;
            user.ReputationLevel = CalculateReputationLevel(user.ReputationScore);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"🎉 Bạn nhận được +{earnedScore} điểm uy tín!";
            return RedirectToAction(nameof(MyReviews));
        }

        // =========================
        // EDIT (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var post = await _context.Posts
                .Include(p => p.PostCategories)
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.PostId == id && !p.IsDeleted);

            if (post == null) return NotFound();
            if (post.Status == PostStatus.Approved)
            {
                TempData["Error"] = "Bài viết đã được duyệt và không thể chỉnh sửa.";
                return RedirectToAction(nameof(MyReviews));
            }
            var model = new ReviewFormViewModel
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                Location = post.Location,
                Rating = post.ReputationImpact,
                SelectedCategoryId = post.PostCategories.First().CategoryId,

                ExistingImages = post.PostImages
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new ReviewImageVm
                    {
                        ImageId = i.PostImageId,
                        ImageUrl = i.ImageUrl
                    }).ToList(),

                AllCategories = await _context.Categories.ToListAsync()
            };

            return View("Create", model);
        }


        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ReviewFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllCategories = await _context.Categories.ToListAsync();
                return View("Create", model);
            }

            var post = await _context.Posts
                .Include(p => p.PostCategories)
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.PostId == model.PostId && !p.IsDeleted);

            if (post == null) return NotFound();

            /* ===== UPDATE POST ===== */
            post.Title = model.Title;
            post.Content = model.Content;
            post.Location = model.Location;
            post.ReputationImpact = model.Rating;

            /* ===== UPDATE CATEGORY ===== */
            _context.PostCategories.RemoveRange(post.PostCategories);
            _context.PostCategories.Add(new PostCategory
            {
                PostId = post.PostId,
                CategoryId = model.SelectedCategoryId
            });

            /* ===== DELETE IMAGES (IF CHECKED) ===== */
            if (model.DeletedImageIds != null && model.DeletedImageIds.Any())
            {
                var toDelete = post.PostImages
                    .Where(i => model.DeletedImageIds.Contains(i.PostImageId))
                    .ToList();

                _context.PostImages.RemoveRange(toDelete);
            }

            /* ===== ADD NEW IMAGES ===== */
            if (model.Images != null && model.Images.Any())
            {
                var uploadDir = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/images/reviews");

                Directory.CreateDirectory(uploadDir);

                int sortOrder = post.PostImages.Any()
                    ? post.PostImages.Max(i => i.SortOrder) + 1
                    : 0;

                foreach (var file in model.Images)
                {
                    if (file.Length == 0) continue;

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var path = Path.Combine(uploadDir, fileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await file.CopyToAsync(stream);

                    post.PostImages.Add(new PostImage
                    {
                        ImageUrl = $"/images/reviews/{fileName}",
                        SortOrder = sortOrder,
                        IsCover = !post.PostImages.Any(),
                        CreatedAt = DateTime.UtcNow
                    });

                    sortOrder++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "✏️ Cập nhật đánh giá thành công!";
            return RedirectToAction(nameof(MyReviews));
        }



        // =========================
        // DELETE (SOFT)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null) return NotFound();

            post.IsDeleted = true;
            post.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "🗑️ Đã xóa đánh giá";
            return RedirectToAction(nameof(MyReviews));
        }

        // =========================
        // REPUTATION LEVEL
        // =========================
        private int CalculateReputationLevel(int score)
        {
            if (score >= 6000) return 5;
            if (score >= 3000) return 4;
            if (score >= 1500) return 3;
            if (score >= 500) return 2;
            return 1;
        }

    }
}
