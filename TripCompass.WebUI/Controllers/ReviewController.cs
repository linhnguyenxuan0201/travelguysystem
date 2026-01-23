using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using TripCompass.Application.Auth;
using TripCompass.Application.Common;
using TripCompass.Application.Features.Comments.CreateComment;
using TripCompass.Application.Interfaces;
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
        private readonly IMediator _mediator;

        public ReviewController(
            IPostRepository postRepository,
            ICurrentUserService currentUser,
            AppDbContext context,
            IMediator mediator)
        {
            _postRepository = postRepository;
            _currentUser = currentUser;
            _context = context;
            _mediator = mediator;
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
                .GroupBy(pc => pc.Category.Slug)
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Match by seeded slugs in DB: du-lich, am-thuc, khach-san
            int travelCount =
                categoryStats.FirstOrDefault(x => x.Category == "du-lich")?.Count ?? 0;

            int foodCount =
                categoryStats.FirstOrDefault(x => x.Category == "am-thuc")?.Count ?? 0;

            int hotelCount =
                categoryStats.FirstOrDefault(x => x.Category == "khach-san")?.Count ?? 0;

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
                TravelCount = travelCount,
                FoodCount = foodCount,
                HotelCount = hotelCount
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
                OpeningHours = model.OpeningHours,
                Phone = model.Phone,
                ParkingInfo = model.ParkingInfo,
                Price = model.Price,
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
            if (post.Status == PostStatus.Published)
            {
                TempData["Error"] = "Bài viết đã được duyệt và không thể chỉnh sửa.";
                return RedirectToAction(nameof(MyReviews));
            }
            var model = new ReviewFormViewModel
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                Location = post.Location ?? string.Empty,
                Rating = post.ReputationImpact,
                OpeningHours = post.OpeningHours,
                Phone = post.Phone,
                ParkingInfo = post.ParkingInfo,
                Price = post.Price,
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
            post.OpeningHours = model.OpeningHours;
            post.Phone = model.Phone;
            post.ParkingInfo = model.ParkingInfo;
            post.Price = model.Price;

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

            var userId = _currentUser.UserId;
            if (post.UserId != userId)
            {
                TempData["Error"] = "Bạn không có quyền xóa bài viết này";
                return RedirectToAction(nameof(MyReviews));
            }

            post.IsDeleted = true;
            post.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log activity
            var appDbContext = _context as IApplicationDbContext;
            if (appDbContext != null)
            {
                await ActivityLogger.LogActivityAsync(
                    appDbContext,
                    userId,
                    "DELETE_OWN_POST",
                    "Posts",
                    post.PostId,
                    $"User deleted own post: {post.Title}");
            }

            TempData["Success"] = "🗑️ Đã xóa đánh giá";
            return RedirectToAction(nameof(MyReviews));
        }

        // =========================
        // DETAIL
        // =========================
        [AllowAnonymous]
        public async Task<IActionResult> Detail(long id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.PostId == id && !p.IsDeleted);

            if (post == null) return NotFound();

            // Load UserRoles và Role riêng để đảm bảo load đúng
            await _context.Entry(post.User)
                .Collection(u => u.UserRoles)
                .Query()
                .Include(ur => ur.Role)
                .LoadAsync();

            // Tăng view count
            post.ViewCount++;
            await _context.SaveChangesAsync();

            // Lấy avatar của author
            var authorAvatar = await _context.UserAvatars
                .Where(a => a.UserId == post.UserId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => a.AvatarUrl)
                .FirstOrDefaultAsync();

            // Đếm số bài viết của author
            var authorPostCount = await _context.Posts
                .CountAsync(p => p.UserId == post.UserId && !p.IsDeleted);

            // Đếm số follower của author
            var authorFollowerCount = await _context.UserFollows
                .CountAsync(f => f.FollowingId == post.UserId);

            // Kiểm tra current user đã follow author chưa (nếu đã đăng nhập)
            bool isFollowing = false;
            try
            {
                var currentUserId = _currentUser.UserId;
                if (currentUserId > 0)
                {
                    isFollowing = await _context.UserFollows
                        .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == post.UserId);
                }
            }
            catch
            {
                // User chưa đăng nhập, isFollowing = false
            }

            // Lấy comments với user info
            var comments = await _context.PostComments
                .Where(c => c.PostId == post.PostId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Take(50)
                .ToListAsync();

            // Lấy tất cả comment IDs
            var commentIds = comments.Select(c => c.Id).ToList();

            // Lấy tất cả reactions cho comments
            var commentReactions = await _context.CommentReactions
                .Where(r => commentIds.Contains(r.CommentId))
                .ToListAsync();

            // Tính like/dislike count cho mỗi comment
            var commentReactionCounts = commentReactions
                .GroupBy(r => r.CommentId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        LikeCount = g.Count(r => r.ReactionType == "LIKE"),
                        DislikeCount = g.Count(r => r.ReactionType == "DISLIKE")
                    }
                );

            // Lấy reactions của current user (nếu đã đăng nhập)
            var currentUserReactions = new Dictionary<long, string>();
            try
            {
                var currentUserId = _currentUser.UserId;
                if (currentUserId > 0)
                {
                    currentUserReactions = await _context.CommentReactions
                        .Where(r => commentIds.Contains(r.CommentId) && r.UserId == currentUserId)
                        .ToDictionaryAsync(r => r.CommentId, r => r.ReactionType);
                }
            }
            catch
            {
                // User chưa đăng nhập
            }

            // Lấy tất cả user IDs từ comments
            var userIds = comments.Select(c => c.UserId).Distinct().ToList();

            // Lấy tất cả users một lần
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId);

            // Lấy tất cả avatars một lần
            var userAvatars = await _context.UserAvatars
                .Where(a => userIds.Contains(a.UserId) && a.IsActive)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, AvatarUrl = g.OrderByDescending(a => a.CreatedAt).First().AvatarUrl })
                .ToDictionaryAsync(a => a.UserId, a => a.AvatarUrl);

            // Tạo dictionary để map comment ID -> CommentViewModel
            var commentViewModelDict = new Dictionary<long, CommentViewModel>();
            var allCommentViewModels = new List<CommentViewModel>();

            foreach (var comment in comments)
            {
                var user = users.GetValueOrDefault(comment.UserId);
                if (user == null) continue; // Skip nếu không tìm thấy user

                var commentUserAvatar = userAvatars.GetValueOrDefault(comment.UserId);

                // Lấy reaction counts
                var reactionCounts = commentReactionCounts.GetValueOrDefault(comment.Id);
                var likeCount = reactionCounts?.LikeCount ?? 0;
                var dislikeCount = reactionCounts?.DislikeCount ?? 0;

                // Check user đã like/dislike chưa
                var userReaction = currentUserReactions.GetValueOrDefault(comment.Id);
                var userLiked = userReaction == "LIKE";
                var userDisliked = userReaction == "DISLIKE";

                // Tính time ago
                var timeSpan = DateTime.UtcNow - comment.CreatedAt;
                string timeAgo;
                if (timeSpan.TotalDays >= 7)
                    timeAgo = $"{(int)timeSpan.TotalDays / 7} tuần trước";
                else if (timeSpan.TotalDays >= 1)
                    timeAgo = $"{(int)timeSpan.TotalDays} ngày trước";
                else if (timeSpan.TotalHours >= 1)
                    timeAgo = $"{(int)timeSpan.TotalHours} giờ trước";
                else
                    timeAgo = $"{(int)timeSpan.TotalMinutes} phút trước";

                var commentVm = new CommentViewModel
                {
                    CommentId = comment.Id,
                    UserId = comment.UserId,
                    UserName = user.UserName,
                    UserAvatar = commentUserAvatar ?? "/images/default-avatar.jpg",
                    Content = comment.Content,
                    Rating = 5, // Default rating
                    LikeCount = likeCount,
                    DislikeCount = dislikeCount,
                    UserLiked = userLiked,
                    UserDisliked = userDisliked,
                    CreatedAt = comment.CreatedAt,
                    TimeAgo = timeAgo,
                    ParentCommentId = comment.ParentCommentId,
                    Replies = new List<CommentViewModel>()
                };

                commentViewModelDict[comment.Id] = commentVm;
                allCommentViewModels.Add(commentVm);
            }

            // Nhóm replies vào comment gốc
            var topLevelComments = new List<CommentViewModel>();
            foreach (var commentVm in allCommentViewModels)
            {
                if (commentVm.ParentCommentId == null)
                {
                    // Top-level comment
                    topLevelComments.Add(commentVm);
                }
                else
                {
                    // Reply - thêm vào Replies của parent
                    if (commentViewModelDict.TryGetValue(commentVm.ParentCommentId.Value, out var parentComment))
                    {
                        parentComment.Replies.Add(commentVm);
                    }
                }
            }

            // Đếm reply count cho mỗi comment
            foreach (var commentVm in allCommentViewModels)
            {
                commentVm.ReplyCount = commentVm.Replies.Count;
            }

            // Sắp xếp: top-level comments theo CreatedAt desc, replies theo CreatedAt asc (cũ nhất trước)
            topLevelComments = topLevelComments.OrderByDescending(c => c.CreatedAt).ToList();
            foreach (var commentVm in allCommentViewModels)
            {
                commentVm.Replies = commentVm.Replies.OrderBy(r => r.CreatedAt).ToList();
            }

            var commentViewModels = topLevelComments;

            // Lấy similar posts (cùng category, khác post hiện tại)
            var categoryIds = post.PostCategories.Select(pc => pc.CategoryId).ToList();
            var similarPosts = await _context.Posts
                .Where(p =>
                    p.PostId != post.PostId &&
                    !p.IsDeleted &&
                    p.Status == PostStatus.Published &&
                    p.PostCategories.Any(pc => categoryIds.Contains(pc.CategoryId)))
                .Include(p => p.PostImages)
                .Include(p => p.PostCategories)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .Select(p => new SimilarPostViewModel
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    ThumbnailUrl = p.PostImages
                        .Where(i => i.IsCover && !i.IsDeleted)
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault() ?? "/images/placeholder.jpg",
                    Rating = p.ReputationImpact,
                    Price = null // Có thể lấy từ metadata nếu có
                })
                .ToListAsync();

            // Tính average rating từ comments (nếu có)
            var averageRating = comments.Any()
                ? (int)Math.Round(comments.Average(c => 5.0)) // Default 5, có thể lấy từ reactions
                : post.ReputationImpact;

            // Tính rating count
            var ratingCount = comments.Count;

            // Kiểm tra nếu author có role Partner
            // Đảm bảo UserRoles đã được load
            var authorRoles = post.User.UserRoles?.Select(ur => ur.Role?.RoleName).ToList() ?? new List<string>();
            var authorHasPartnerRole = authorRoles.Any(role => role == "Partner");

            // Kiểm tra nếu post có thông tin liên hệ (Phone hoặc OpeningHours)
            var hasContactInfo = !string.IsNullOrEmpty(post.Phone) || !string.IsNullOrEmpty(post.OpeningHours);

            // IsPartner = true nếu:
            // 1. post.IsPartner = true, HOẶC
            // 2. author có role Partner, HOẶC  
            // 3. post có thông tin liên hệ (Phone hoặc OpeningHours) - để hiển thị phần đặt chỗ
            var isPartner = post.IsPartner || authorHasPartnerRole || hasContactInfo;

            var vm = new ReviewDetailViewModel
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                Location = post.Location,
                Status = post.Status,
                Rating = post.ReputationImpact,
                Price = post.Price,
                IsPartner = isPartner,

                ViewCount = post.ViewCount,
                LikeCount = post.LikeCount,
                DislikeCount = post.DislikeCount,
                CommentCount = comments.Count,

                CreatedAt = post.CreatedAt,
                PublishedAt = post.PublishedAt,

                AuthorId = post.UserId,
                AuthorName = post.User.UserName,
                AuthorAvatar = authorAvatar ?? "/images/default-avatar.jpg",
                AuthorReputationScore = post.User.ReputationScore,
                AuthorReputationLevel = post.User.ReputationLevel,
                AuthorPostCount = authorPostCount,
                AuthorFollowerCount = authorFollowerCount,
                AuthorBio = null, // Có thể thêm vào User entity nếu cần
                IsFollowing = isFollowing,

                Categories = post.PostCategories.Select(pc => pc.Category.Name).ToList(),
                Images = post.PostImages
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl)
                    .ToList(),
                CoverImage = post.PostImages
                    .Where(i => i.IsCover && !i.IsDeleted)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault() ?? "/images/placeholder.jpg",

                OpeningHours = post.OpeningHours,
                Address = post.Location,
                Phone = post.Phone,
                ParkingInfo = post.ParkingInfo,

                Comments = commentViewModels,
                SimilarPosts = similarPosts
            };

            return View(vm);
        }

        // =========================
        // SUBMIT COMMENT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComment(long postId, string content, long? parentCommentId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Vui lòng nhập nội dung đánh giá";
                return RedirectToAction(nameof(Detail), new { id = postId });
            }

            var userId = _currentUser.UserId;

            try
            {
                var command = new CreateCommentCommand(postId, userId, content, parentCommentId);
                await _mediator.Send(command);

                TempData["Success"] = parentCommentId.HasValue
                    ? "🎉 Phản hồi của bạn đã được gửi!"
                    : "🎉 Đánh giá của bạn đã được gửi! Bạn đã nhận được coin và điểm uy tín.";
                return RedirectToAction(nameof(Detail), new { id = postId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(Detail), new { id = postId });
            }
        }

        // =========================
        // COMMENT REACTION (LIKE/DISLIKE)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCommentReaction(long commentId, string reactionType)
        {
            var currentUserId = _currentUser.UserId;

            if (reactionType != "LIKE" && reactionType != "DISLIKE")
            {
                return Json(new { success = false, message = "Invalid reaction type" });
            }

            var existingReaction = await _context.CommentReactions
                .FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == currentUserId);

            var comment = await _context.PostComments
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found" });
            }

            // Lấy user của comment để cập nhật uy tín
            var commentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == comment.UserId);

            if (commentUser == null)
            {
                return Json(new { success = false, message = "Comment user not found" });
            }

            if (existingReaction != null)
            {
                // Nếu đã react cùng loại, thì bỏ react
                if (existingReaction.ReactionType == reactionType)
                {
                    _context.CommentReactions.Remove(existingReaction);

                    // Cập nhật uy tín của người comment
                    if (reactionType == "LIKE")
                    {
                        commentUser.ReputationScore = Math.Max(0, commentUser.ReputationScore - 2);
                    }
                    else
                    {
                        commentUser.ReputationScore = Math.Max(0, commentUser.ReputationScore + 1);
                    }
                }
                else
                {
                    // Đổi từ LIKE sang DISLIKE hoặc ngược lại
                    var oldType = existingReaction.ReactionType;
                    existingReaction.ReactionType = reactionType;
                    _context.CommentReactions.Update(existingReaction);

                    // Cập nhật uy tín
                    if (oldType == "LIKE" && reactionType == "DISLIKE")
                    {
                        commentUser.ReputationScore = Math.Max(0, commentUser.ReputationScore - 3); // -2 (bỏ like) -1 (thêm dislike)
                    }
                    else if (oldType == "DISLIKE" && reactionType == "LIKE")
                    {
                        commentUser.ReputationScore = Math.Max(0, commentUser.ReputationScore + 3); // +1 (bỏ dislike) +2 (thêm like)
                    }
                }
            }
            else
            {
                // Thêm reaction mới
                var reaction = new CommentReaction
                {
                    CommentId = commentId,
                    UserId = currentUserId,
                    ReactionType = reactionType,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CommentReactions.Add(reaction);

                // Cập nhật uy tín của người comment
                if (reactionType == "LIKE")
                {
                    commentUser.ReputationScore = Math.Max(0, commentUser.ReputationScore + 2);
                }
                else
                {
                    commentUser.ReputationScore = Math.Max(0, commentUser.ReputationScore - 1);
                }
            }

            // Cập nhật reputation level
            commentUser.ReputationLevel = CalculateReputationLevel(commentUser.ReputationScore);
            await _context.SaveChangesAsync();

            // Log activity
            var actionType = reactionType == "LIKE" ? "LIKE_COMMENT" : "DISLIKE_COMMENT";
            if (existingReaction != null && existingReaction.ReactionType == reactionType)
            {
                // Removed reaction - log as opposite action
                actionType = reactionType == "LIKE" ? "DISLIKE_COMMENT" : "LIKE_COMMENT";
            }
            
            var appDbContext = _context as IApplicationDbContext;
            if (appDbContext != null)
            {
                await ActivityLogger.LogActivityAsync(
                    appDbContext,
                    currentUserId,
                    actionType,
                    "CommentReactions",
                    commentId,
                    $"User {actionType.ToLower()} on comment ID: {commentId}");
            }

            // Lấy counts mới
            var likeCount = await _context.CommentReactions
                .CountAsync(r => r.CommentId == commentId && r.ReactionType == "LIKE");
            var dislikeCount = await _context.CommentReactions
                .CountAsync(r => r.CommentId == commentId && r.ReactionType == "DISLIKE");

            // Check user đã like/dislike chưa
            var userReaction = await _context.CommentReactions
                .FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == currentUserId);

            return Json(new
            {
                success = true,
                likeCount = likeCount,
                dislikeCount = dislikeCount,
                userLiked = userReaction?.ReactionType == "LIKE",
                userDisliked = userReaction?.ReactionType == "DISLIKE"
            });
        }

        // =========================
        // FOLLOW/UNFOLLOW
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow(long authorId)
        {
            var currentUserId = _currentUser.UserId;

            if (currentUserId == authorId)
            {
                return Json(new { success = false, message = "Không thể theo dõi chính mình" });
            }

            var existingFollow = await _context.UserFollows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == authorId);

            if (existingFollow != null)
            {
                // Unfollow
                _context.UserFollows.Remove(existingFollow);
                await _context.SaveChangesAsync();

                // Log activity
                var appDbContext = _context as IApplicationDbContext;
                if (appDbContext != null)
                {
                    await ActivityLogger.LogActivityAsync(
                        appDbContext,
                        currentUserId,
                        "UNFOLLOW_USER",
                        "UserFollows",
                        authorId,
                        $"User unfollowed user ID: {authorId}");
                }

                var newFollowerCount = await _context.UserFollows
                    .CountAsync(f => f.FollowingId == authorId);

                return Json(new
                {
                    success = true,
                    isFollowing = false,
                    followerCount = newFollowerCount,
                    message = "Đã bỏ theo dõi"
                });
            }
            else
            {
                // Follow
                var follow = new UserFollow
                {
                    FollowerId = currentUserId,
                    FollowingId = authorId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserFollows.Add(follow);
                await _context.SaveChangesAsync();

                // Log activity
                var appDbContext = _context as IApplicationDbContext;
                if (appDbContext != null)
                {
                    await ActivityLogger.LogActivityAsync(
                        appDbContext,
                        currentUserId,
                        "FOLLOW_USER",
                        "UserFollows",
                        authorId,
                        $"User followed user ID: {authorId}");
                }

                var newFollowerCount = await _context.UserFollows
                    .CountAsync(f => f.FollowingId == authorId);

                return Json(new
                {
                    success = true,
                    isFollowing = true,
                    followerCount = newFollowerCount,
                    message = "Đã theo dõi"
                });
            }
        }

        // =========================
        // REVIEWS PAGE (PUBLIC)
        // =========================
        [AllowAnonymous]
        public async Task<IActionResult> Reviews(string? category, bool? partnerOnly, string? search, string? province, string? priceRange, string? sortBy)
        {
            var vm = new ReviewsPageViewModel
            {
                SelectedCategory = category,
                ShowPartnerOnly = partnerOnly,
                SearchQuery = search,
                SelectedProvince = province,
                SelectedPriceRange = priceRange,
                SortBy = sortBy ?? "popular"
            };

            // Lấy tất cả categories
            var categories = await _context.Categories.ToListAsync();
            vm.Categories = categories;

            // Lấy tất cả provinces từ Location của posts
            var provinces = await _context.Posts
                .Where(p => !p.IsDeleted && p.Status == PostStatus.Published && !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location!)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
            vm.Provinces = provinces;

            // Tính stats
            var allPublishedPosts = _context.Posts
                .Where(p => !p.IsDeleted && p.Status == PostStatus.Published);

            vm.TotalProducts = await allPublishedPosts.CountAsync();

            var avgRating = await allPublishedPosts
                .Where(p => p.ReputationImpact > 0)
                .AverageAsync(p => (double?)p.ReputationImpact) ?? 0;
            vm.AverageRating = Math.Round(avgRating, 1);

            // Nếu không có products, set default
            if (vm.TotalProducts == 0)
            {
                vm.TotalProducts = 0;
                vm.AverageRating = 0.0;
            }

            // Query base: chỉ lấy posts Published và không bị xóa
            var baseQuery = _context.Posts
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.PostImages)
                .Include(p => p.User)
                .Where(p => !p.IsDeleted && p.Status == PostStatus.Published);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery = baseQuery.Where(p =>
                    p.Title.Contains(search) ||
                    p.Content.Contains(search) ||
                    p.Location != null && p.Location.Contains(search));
            }

            // Apply province filter - exact match or starts with
            if (!string.IsNullOrWhiteSpace(province))
            {
                baseQuery = baseQuery.Where(p => p.Location != null &&
                    (p.Location == province || p.Location.StartsWith(province + ",") || p.Location.StartsWith(province + " ")));
            }

            // Apply price range filter
            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                switch (priceRange)
                {
                    case "under-1m":
                        baseQuery = baseQuery.Where(p => p.Price.HasValue && p.Price < 1000000);
                        break;
                    case "1m-3m":
                        baseQuery = baseQuery.Where(p => p.Price.HasValue && p.Price >= 1000000 && p.Price < 3000000);
                        break;
                    case "3m-5m":
                        baseQuery = baseQuery.Where(p => p.Price.HasValue && p.Price >= 3000000 && p.Price < 5000000);
                        break;
                    case "over-5m":
                        baseQuery = baseQuery.Where(p => p.Price.HasValue && p.Price >= 5000000);
                        break;
                }
            }

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                var selectedCategory = categories.FirstOrDefault(c => c.Slug == category);
                if (selectedCategory != null)
                {
                    baseQuery = baseQuery.Where(p => p.PostCategories.Any(pc => pc.CategoryId == selectedCategory.CategoryId));
                }
            }

            // Apply partner filter
            if (partnerOnly == true)
            {
                baseQuery = baseQuery.Where(p => p.IsPartner);
            }

            // Apply sort
            switch (sortBy)
            {
                case "popular":
                    baseQuery = baseQuery.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ReputationImpact);
                    break;
                case "rating":
                    baseQuery = baseQuery.OrderByDescending(p => p.ReputationImpact);
                    break;
                case "price-low":
                    baseQuery = baseQuery.OrderBy(p => p.Price ?? decimal.MaxValue);
                    break;
                case "price-high":
                    baseQuery = baseQuery.OrderByDescending(p => p.Price ?? 0);
                    break;
                case "newest":
                    baseQuery = baseQuery.OrderByDescending(p => p.CreatedAt);
                    break;
                default:
                    baseQuery = baseQuery.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ReputationImpact);
                    break;
            }

            // =========================
            // FEATURED POSTS
            // Nếu category = null: lấy posts không có category
            // Nếu category có giá trị: lấy posts thuộc category đó (không phải đối tác)
            // =========================
            var featuredBaseQuery = _context.Posts
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.PostImages)
                .Include(p => p.User)
                .Where(p => !p.IsDeleted && p.Status == PostStatus.Published);

            // Apply category filter
            if (string.IsNullOrWhiteSpace(category))
            {
                // "Tất cả": Lấy posts không có category
                featuredBaseQuery = featuredBaseQuery.Where(p => !p.PostCategories.Any());
            }
            else
            {
                // Category cụ thể: Lấy posts thuộc category đó
                var selectedCategory = categories.FirstOrDefault(c => c.Slug == category);
                if (selectedCategory != null)
                {
                    featuredBaseQuery = featuredBaseQuery.Where(p => p.PostCategories.Any(pc => pc.CategoryId == selectedCategory.CategoryId));
                }
            }

            // Featured posts không phải đối tác
            featuredBaseQuery = featuredBaseQuery.Where(p => !p.IsPartner);

            // Apply search filter to featured
            if (!string.IsNullOrWhiteSpace(search))
            {
                featuredBaseQuery = featuredBaseQuery.Where(p =>
                    p.Title.Contains(search) ||
                    p.Content.Contains(search) ||
                    p.Location != null && p.Location.Contains(search));
            }

            // Apply province filter to featured - exact match or starts with
            if (!string.IsNullOrWhiteSpace(province))
            {
                featuredBaseQuery = featuredBaseQuery.Where(p => p.Location != null &&
                    (p.Location == province || p.Location.StartsWith(province + ",") || p.Location.StartsWith(province + " ")));
            }

            // Apply price filter to featured
            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                switch (priceRange)
                {
                    case "under-1m":
                        featuredBaseQuery = featuredBaseQuery.Where(p => p.Price.HasValue && p.Price < 1000000);
                        break;
                    case "1m-3m":
                        featuredBaseQuery = featuredBaseQuery.Where(p => p.Price.HasValue && p.Price >= 1000000 && p.Price < 3000000);
                        break;
                    case "3m-5m":
                        featuredBaseQuery = featuredBaseQuery.Where(p => p.Price.HasValue && p.Price >= 3000000 && p.Price < 5000000);
                        break;
                    case "over-5m":
                        featuredBaseQuery = featuredBaseQuery.Where(p => p.Price.HasValue && p.Price >= 5000000);
                        break;
                }
            }

            var featuredPosts = await featuredBaseQuery
                .OrderByDescending(p => p.ReputationImpact)
                .ThenByDescending(p => p.ViewCount)
                .Take(10)
                .ToListAsync();

            // Nếu "Tất cả" và không có posts không có category, lấy tất cả posts (có category cũng được)
            if (string.IsNullOrWhiteSpace(category) && !featuredPosts.Any())
            {
                var allFeaturedQuery = _context.Posts
                    .Include(p => p.PostCategories)
                        .ThenInclude(pc => pc.Category)
                    .Include(p => p.PostImages)
                    .Include(p => p.User)
                    .Where(p => !p.IsDeleted && p.Status == PostStatus.Published)
                    .Where(p => !p.IsPartner);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    allFeaturedQuery = allFeaturedQuery.Where(p =>
                        p.Title.Contains(search) ||
                        p.Content.Contains(search) ||
                        p.Location != null && p.Location.Contains(search));
                }

                // Apply province filter
                if (!string.IsNullOrWhiteSpace(province))
                {
                    allFeaturedQuery = allFeaturedQuery.Where(p => p.Location != null &&
                        (p.Location == province || p.Location.StartsWith(province + ",") || p.Location.StartsWith(province + " ")));
                }

                // Apply price filter
                if (!string.IsNullOrWhiteSpace(priceRange))
                {
                    switch (priceRange)
                    {
                        case "under-1m":
                            allFeaturedQuery = allFeaturedQuery.Where(p => p.Price.HasValue && p.Price < 1000000);
                            break;
                        case "1m-3m":
                            allFeaturedQuery = allFeaturedQuery.Where(p => p.Price.HasValue && p.Price >= 1000000 && p.Price < 3000000);
                            break;
                        case "3m-5m":
                            allFeaturedQuery = allFeaturedQuery.Where(p => p.Price.HasValue && p.Price >= 3000000 && p.Price < 5000000);
                            break;
                        case "over-5m":
                            allFeaturedQuery = allFeaturedQuery.Where(p => p.Price.HasValue && p.Price >= 5000000);
                            break;
                    }
                }

                featuredPosts = await allFeaturedQuery
                    .OrderByDescending(p => p.ReputationImpact)
                    .ThenByDescending(p => p.ViewCount)
                    .Take(10)
                    .ToListAsync();
            }

            vm.FeaturedPosts = await MapToReviewItems(featuredPosts);

            // =========================
            // PARTNER POSTS
            // Nếu category = null: lấy posts đối tác không có category
            // Nếu category có giá trị: lấy posts đối tác thuộc category đó
            // =========================
            var partnerBaseQuery = _context.Posts
                .Include(p => p.PostCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.PostImages)
                .Include(p => p.User)
                .Where(p => !p.IsDeleted && p.Status == PostStatus.Published)
                .Where(p => p.IsPartner);

            // Apply category filter
            if (string.IsNullOrWhiteSpace(category))
            {
                // "Tất cả": Lấy posts đối tác không có category
                partnerBaseQuery = partnerBaseQuery.Where(p => !p.PostCategories.Any());
            }
            else
            {
                // Category cụ thể: Lấy posts đối tác thuộc category đó
                var selectedCategory = categories.FirstOrDefault(c => c.Slug == category);
                if (selectedCategory != null)
                {
                    partnerBaseQuery = partnerBaseQuery.Where(p => p.PostCategories.Any(pc => pc.CategoryId == selectedCategory.CategoryId));
                }
            }

            // Apply filters to partner
            if (!string.IsNullOrWhiteSpace(search))
            {
                partnerBaseQuery = partnerBaseQuery.Where(p =>
                    p.Title.Contains(search) ||
                    p.Content.Contains(search) ||
                    p.Location != null && p.Location.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(province))
            {
                partnerBaseQuery = partnerBaseQuery.Where(p => p.Location != null &&
                    (p.Location == province || p.Location.StartsWith(province + ",") || p.Location.StartsWith(province + " ")));
            }
            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                switch (priceRange)
                {
                    case "under-1m":
                        partnerBaseQuery = partnerBaseQuery.Where(p => p.Price.HasValue && p.Price < 1000000);
                        break;
                    case "1m-3m":
                        partnerBaseQuery = partnerBaseQuery.Where(p => p.Price.HasValue && p.Price >= 1000000 && p.Price < 3000000);
                        break;
                    case "3m-5m":
                        partnerBaseQuery = partnerBaseQuery.Where(p => p.Price.HasValue && p.Price >= 3000000 && p.Price < 5000000);
                        break;
                    case "over-5m":
                        partnerBaseQuery = partnerBaseQuery.Where(p => p.Price.HasValue && p.Price >= 5000000);
                        break;
                }
            }

            var partnerPosts = await partnerBaseQuery
                .OrderByDescending(p => p.ReputationImpact)
                .ThenByDescending(p => p.ViewCount)
                .Take(10)
                .ToListAsync();

            // Nếu "Tất cả" và không có posts đối tác không có category, lấy tất cả posts đối tác (có category cũng được)
            if (string.IsNullOrWhiteSpace(category) && !partnerPosts.Any())
            {
                var allPartnerQuery = _context.Posts
                    .Include(p => p.PostCategories)
                        .ThenInclude(pc => pc.Category)
                    .Include(p => p.PostImages)
                    .Include(p => p.User)
                    .Where(p => !p.IsDeleted && p.Status == PostStatus.Published)
                    .Where(p => p.IsPartner);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    allPartnerQuery = allPartnerQuery.Where(p =>
                        p.Title.Contains(search) ||
                        p.Content.Contains(search) ||
                        p.Location != null && p.Location.Contains(search));
                }

                // Apply province filter
                if (!string.IsNullOrWhiteSpace(province))
                {
                    allPartnerQuery = allPartnerQuery.Where(p => p.Location != null &&
                        (p.Location == province || p.Location.StartsWith(province + ",") || p.Location.StartsWith(province + " ")));
                }

                // Apply price filter
                if (!string.IsNullOrWhiteSpace(priceRange))
                {
                    switch (priceRange)
                    {
                        case "under-1m":
                            allPartnerQuery = allPartnerQuery.Where(p => p.Price.HasValue && p.Price < 1000000);
                            break;
                        case "1m-3m":
                            allPartnerQuery = allPartnerQuery.Where(p => p.Price.HasValue && p.Price >= 1000000 && p.Price < 3000000);
                            break;
                        case "3m-5m":
                            allPartnerQuery = allPartnerQuery.Where(p => p.Price.HasValue && p.Price >= 3000000 && p.Price < 5000000);
                            break;
                        case "over-5m":
                            allPartnerQuery = allPartnerQuery.Where(p => p.Price.HasValue && p.Price >= 5000000);
                            break;
                    }
                }

                partnerPosts = await allPartnerQuery
                    .OrderByDescending(p => p.ReputationImpact)
                    .ThenByDescending(p => p.ViewCount)
                    .Take(10)
                    .ToListAsync();
            }

            vm.PartnerPosts = await MapToReviewItems(partnerPosts);

            // Apply category filter for category sections
            if (!string.IsNullOrWhiteSpace(category))
            {
                var selectedCategory = categories.FirstOrDefault(c => c.Slug == category);
                if (selectedCategory != null)
                {
                    baseQuery = baseQuery.Where(p => p.PostCategories.Any(pc => pc.CategoryId == selectedCategory.CategoryId));
                }
            }

            // Apply partner filter for category sections
            if (partnerOnly == true)
            {
                baseQuery = baseQuery.Where(p => p.IsPartner);
            }

            // Apply sort for category sections
            switch (sortBy)
            {
                case "popular":
                    baseQuery = baseQuery.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ReputationImpact);
                    break;
                case "rating":
                    baseQuery = baseQuery.OrderByDescending(p => p.ReputationImpact);
                    break;
                case "price-low":
                    baseQuery = baseQuery.OrderBy(p => p.Price ?? decimal.MaxValue);
                    break;
                case "price-high":
                    baseQuery = baseQuery.OrderByDescending(p => p.Price ?? 0);
                    break;
                case "newest":
                    baseQuery = baseQuery.OrderByDescending(p => p.CreatedAt);
                    break;
                default:
                    baseQuery = baseQuery.OrderByDescending(p => p.ViewCount).ThenByDescending(p => p.ReputationImpact);
                    break;
            }

            // =========================
            // CATEGORY SECTIONS
            // Luôn hiển thị, nếu có filter category thì chỉ hiển thị category đó
            // =========================
            var categoriesToShow = !string.IsNullOrWhiteSpace(category)
                ? categories.Where(c => c.Slug == category).ToList()
                : categories;

            foreach (var cat in categoriesToShow)
            {
                var categoryPostsQuery = _context.Posts
                    .Include(p => p.PostCategories)
                        .ThenInclude(pc => pc.Category)
                    .Include(p => p.PostImages)
                    .Include(p => p.User)
                    .Where(p => !p.IsDeleted && p.Status == PostStatus.Published)
                    .Where(p => p.PostCategories.Any(pc => pc.CategoryId == cat.CategoryId));

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    categoryPostsQuery = categoryPostsQuery.Where(p =>
                        p.Title.Contains(search) ||
                        p.Content.Contains(search) ||
                        p.Location != null && p.Location.Contains(search));
                }

                // Apply province filter
                if (!string.IsNullOrWhiteSpace(province))
                {
                    categoryPostsQuery = categoryPostsQuery.Where(p => p.Location != null &&
                        (p.Location == province || p.Location.StartsWith(province + ",") || p.Location.StartsWith(province + " ")));
                }

                // Apply price filter
                if (!string.IsNullOrWhiteSpace(priceRange))
                {
                    switch (priceRange)
                    {
                        case "under-1m":
                            categoryPostsQuery = categoryPostsQuery.Where(p => p.Price.HasValue && p.Price < 1000000);
                            break;
                        case "1m-3m":
                            categoryPostsQuery = categoryPostsQuery.Where(p => p.Price.HasValue && p.Price >= 1000000 && p.Price < 3000000);
                            break;
                        case "3m-5m":
                            categoryPostsQuery = categoryPostsQuery.Where(p => p.Price.HasValue && p.Price >= 3000000 && p.Price < 5000000);
                            break;
                        case "over-5m":
                            categoryPostsQuery = categoryPostsQuery.Where(p => p.Price.HasValue && p.Price >= 5000000);
                            break;
                    }
                }

                // Featured posts in this category
                var categoryFeatured = await categoryPostsQuery
                    .Where(p => !p.IsPartner)
                    .OrderByDescending(p => p.ReputationImpact)
                    .ThenByDescending(p => p.ViewCount)
                    .Take(10)
                    .ToListAsync();

                // Partner posts in this category
                var categoryPartner = await categoryPostsQuery
                    .Where(p => p.IsPartner)
                    .OrderByDescending(p => p.ReputationImpact)
                    .ThenByDescending(p => p.ViewCount)
                    .Take(10)
                    .ToListAsync();

                vm.CategorySections.Add(new CategorySectionViewModel
                {
                    CategoryId = cat.CategoryId,
                    CategoryName = cat.Name,
                    CategorySlug = cat.Slug,
                    FeaturedPosts = await MapToReviewItems(categoryFeatured),
                    PartnerPosts = await MapToReviewItems(categoryPartner)
                });
            }

            // Apply partner filter if selected
            if (partnerOnly == true)
            {
                vm.FeaturedPosts = new List<ReviewItemViewModel>();
                foreach (var section in vm.CategorySections)
                {
                    section.FeaturedPosts = new List<ReviewItemViewModel>();
                }
            }

            return View(vm);
        }

        // =========================
        // MAP POSTS TO REVIEW ITEMS
        // =========================
        private async Task<List<ReviewItemViewModel>> MapToReviewItems(List<Post> posts)
        {
            var result = new List<ReviewItemViewModel>();

            if (!posts.Any()) return result;

            var postIds = posts.Select(p => p.PostId).ToList();
            var userIds = posts.Select(p => p.UserId).Distinct().ToList();

            // Lấy comment counts
            var commentCounts = await _context.PostComments
                .Where(c => postIds.Contains(c.PostId) && !c.IsDeleted)
                .GroupBy(c => c.PostId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // Lấy avatars
            var userAvatars = await _context.UserAvatars
                .Where(a => userIds.Contains(a.UserId) && a.IsActive)
                .GroupBy(a => a.UserId)
                .Select(g => new { UserId = g.Key, AvatarUrl = g.OrderByDescending(a => a.CreatedAt).First().AvatarUrl })
                .ToDictionaryAsync(a => a.UserId, a => a.AvatarUrl);

            foreach (var post in posts)
            {
                var thumbnail = post.PostImages
                    .OrderBy(pi => pi.SortOrder)
                    .FirstOrDefault()?.ImageUrl;

                result.Add(new ReviewItemViewModel
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Location = post.Location,
                    ThumbnailUrl = thumbnail ?? "/images/default-thumbnail.jpg",
                    Rating = post.ReputationImpact,
                    ViewCount = post.ViewCount,
                    CommentCount = commentCounts.GetValueOrDefault(post.PostId, 0),
                    Price = post.Price,
                    ReputationImpact = post.ReputationImpact,
                    IsPartner = post.IsPartner,
                    CreatedAt = post.CreatedAt,
                    AuthorName = post.User.UserName,
                    AuthorAvatar = userAvatars.GetValueOrDefault(post.UserId) ?? "/images/default-avatar.jpg"
                });
            }

            return result;
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

        // =========================
        // REGISTER AS PARTNER
        // =========================
        // PARTNER REGISTRATION FORM
        // =========================
        [HttpGet]
        [Authorize]
        public IActionResult PartnerRegistration()
        {
            return View(new PartnerRegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SubmitPartnerRegistration(PartnerRegistrationViewModel model)
        {
            // Check if it's an AJAX request
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                         Request.Headers["Accept"].ToString().Contains("application/json");

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join("\n", errors) });
                }
                return View("PartnerRegistration", model);
            }

            if (!model.AgreeToTerms)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Bạn phải đồng ý với điều khoản sử dụng." });
                }
                ModelState.AddModelError("AgreeToTerms", "Bạn phải đồng ý với điều khoản sử dụng.");
                return View("PartnerRegistration", model);
            }

            try
            {
                var userId = _currentUser.UserId;
                if (userId <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đăng ký làm đối tác." });
                }

                // Kiểm tra xem user đã là đối tác chưa
                var partnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Partner");
                if (partnerRole == null)
                {
                    return Json(new { success = false, message = "Role Partner chưa được tạo trong hệ thống." });
                }

                var isAlreadyPartner = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == userId && ur.RoleId == partnerRole.RoleId);

                if (isAlreadyPartner)
                {
                    return Json(new { success = false, message = "Bạn đã là đối tác của TripCompass." });
                }

                // Kiểm tra xem đã có thông tin partner chưa
                var existingPartner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (existingPartner != null)
                {
                    return Json(new { success = false, message = "Bạn đã đăng ký thông tin đối tác rồi." });
                }

                // Lưu thông tin đồng ý điều khoản
                var agreement = new PartnerAgreement
                {
                    UserId = userId,
                    AgreementVersion = "v1.0",
                    AgreedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };

                _context.PartnerAgreements.Add(agreement);

                // Lưu thông tin đối tác
                var partner = new Partner
                {
                    UserId = userId,
                    StoreName = model.StoreName,
                    BusinessType = model.BusinessType,
                    RepresentativeName = model.RepresentativeName,
                    PhoneNumber = model.PhoneNumber,
                    BusinessAddress = model.BusinessAddress,
                    BankName = model.BankName,
                    AccountNumber = model.AccountNumber,
                    AccountHolderName = model.AccountHolderName,
                    IdNumber = model.IdNumber,
                    TaxId = model.TaxId,
                    ServiceDescription = model.ServiceDescription,
                    IsApproved = false, // Cần admin phê duyệt
                    CreatedAt = DateTime.UtcNow
                };

                _context.Partners.Add(partner);

                // Gán role Partner cho user
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = partnerRole.RoleId
                };

                _context.UserRoles.Add(userRole);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đăng ký làm đối tác thành công! Vui lòng đăng xuất và đăng nhập lại để cập nhật quyền đối tác.",
                    requireRelogin = true
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        

    }
}
