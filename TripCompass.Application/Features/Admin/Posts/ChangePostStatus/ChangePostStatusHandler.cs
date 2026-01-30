using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Common;
using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Posts.ChangePostStatus
{
    public class ChangePostStatusHandler : IRequestHandler<ChangePostStatusCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly TripCompass.Application.Services.NotificationService _notificationService;
        private readonly IUnitOfWork _uow;

        public ChangePostStatusHandler(
            IApplicationDbContext context, 
            ICurrentUserService currentUser,
            TripCompass.Application.Services.NotificationService notificationService,
            IUnitOfWork uow)
        {
            _context = context;
            _currentUser = currentUser;
            _notificationService = notificationService;
            _uow = uow;
        }

        public async Task<bool> Handle(ChangePostStatusCommand request, CancellationToken cancellationToken)
        {
            var post = await _context.Posts.FindAsync(new object[] { request.PostId }, cancellationToken);

            if (post == null) return false;

            var adminId = _currentUser.UserId;
            
            // Nếu là admin từ config (UserId = 0), tìm một admin user trong database để dùng cho logging
            if (adminId == 0 && _currentUser.IsConfigAdmin())
            {
                var adminUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "Admin"))
                    .OrderBy(u => u.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (adminUser != null)
                {
                    adminId = adminUser.UserId;
                }
                else
                {
                    // Nếu không tìm thấy admin user nào, skip logging nhưng vẫn cho phép thực hiện action
                    adminId = 0;
                }
            }
            else if (adminId == 0)
            {
                throw new UnauthorizedAccessException("Admin ID not found");
            }

            var oldStatus = post.Status;
            var oldIsDeleted = post.IsDeleted;
            var actionType = "";
            var note = "";

            // Xử lý soft delete (Deleted có thể từ bất kỳ status nào)
            if (request.IsDeleted.HasValue)
            {
                post.IsDeleted = request.IsDeleted.Value;
                if (request.IsDeleted.Value)
                {
                    post.DeletedAt = DateTime.UtcNow;
                    actionType = "DELETE_POST";
                    note = $"Deleted post: {post.Title}";
                }
                else
                {
                    post.DeletedAt = null;
                    actionType = "RESTORE_POST";
                    note = $"Restored post: {post.Title}";
                }
            }

            // Nếu có NewStatus, validate và update
            if (request.NewStatus.HasValue)
            {
                // Validate workflow transition
                if (!PostStatusWorkflow.IsValidTransition(post.Status, request.NewStatus.Value))
                {
                    throw new InvalidOperationException(
                        $"Invalid status transition from {post.Status} to {request.NewStatus.Value}. " +
                        $"Valid transitions: {string.Join(", ", PostStatusWorkflow.GetValidNextStatuses(post.Status))}");
                }

                // Không cho phép Published trực tiếp từ Draft
                if (post.Status == PostStatus.Draft && request.NewStatus.Value == PostStatus.Published)
                {
                    throw new InvalidOperationException(
                        "Cannot publish directly from Draft. Post must go through PendingApproval first.");
                }

                post.Status = request.NewStatus.Value;
                
                // If publishing, set PublishedAt
                if (request.NewStatus.Value == PostStatus.Published && post.PublishedAt == null)
                {
                    post.PublishedAt = DateTime.UtcNow;
                }

                // Determine action type based on status change
                if (string.IsNullOrEmpty(actionType))
                {
                    actionType = request.NewStatus.Value switch
                    {
                        PostStatus.Published => "APPROVE_POST",
                        PostStatus.Rejected => "REJECT_POST",
                        PostStatus.Archived => "ARCHIVE_POST",
                        PostStatus.Pending => "CHANGE_POST_STATUS",
                        _ => "CHANGE_POST_STATUS"
                    };
                }

                if (string.IsNullOrEmpty(note))
                {
                    note = $"Changed post status from {oldStatus} to {request.NewStatus.Value}. Post: {post.Title}";
                    if (!string.IsNullOrEmpty(request.ModerationNote))
                    {
                        note += $". Note: {request.ModerationNote}";
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(request.ModerationNote))
            {
                post.ModerationNote = request.ModerationNote;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Tạo thông báo cho author khi bài viết được duyệt hoặc từ chối
            if (request.NewStatus.HasValue && post.UserId > 0)
            {
                if (request.NewStatus.Value == PostStatus.Published)
                {
                    // Chỉ cộng reputation và coin khi bài viết được publish
                    // Kiểm tra xem bài viết đã từng được publish chưa (tránh cộng lại khi republish)
                    bool isFirstTimePublished = oldStatus != PostStatus.Published;
                    
                    if (isFirstTimePublished)
                    {
                        // Load user
                        var user = await _uow.Users.GetByIdAsync(post.UserId);
                        if (user != null)
                        {
                            // Cộng reputation
                            int earnedScore = new Random().Next(50, 201);
                            user.ReputationScore += earnedScore;
                            user.ReputationLevel = CalculateReputationLevel(user.ReputationScore);
                            
                            // Cộng coin (nếu có wallet)
                            var wallet = await _uow.Wallets.GetByUserIdAsync(post.UserId);
                            int coinEarned = 0;
                            if (wallet != null)
                            {
                                coinEarned = new Random().Next(50, 201); // 50-200 coin
                                wallet.Balance = Math.Max(0, wallet.Balance + coinEarned);
                            }
                            
                            await _uow.SaveChangesAsync(cancellationToken);
                            
                            // Thông báo cộng reputation
                            await _notificationService.CreateNotificationAsync(
                                post.UserId,
                                "REPUTATION_EARNED",
                                "Bạn đã nhận được điểm uy tín!",
                                $"Bạn nhận được +{earnedScore} điểm uy tín từ bài viết \"{post.Title}\"",
                                $"/Review/Detail/{post.PostId}",
                                post.PostId, // ReferenceId = PostId
                                cancellationToken
                            );
                            
                            // Thông báo cộng coin (nếu có)
                            if (coinEarned > 0)
                            {
                                await _notificationService.CreateNotificationAsync(
                                    post.UserId,
                                    "COIN_EARNED",
                                    "Bạn đã nhận được coin!",
                                    $"Bạn nhận được +{coinEarned} coin từ bài viết \"{post.Title}\"",
                                    $"/Review/Detail/{post.PostId}",
                                    post.PostId, // ReferenceId = PostId
                                    cancellationToken
                                );
                            }
                        }
                    }
                    
                    await _notificationService.CreateNotificationAsync(
                        post.UserId,
                        "POST_APPROVED",
                        "Bài viết của bạn đã được duyệt",
                        $"Bài viết \"{post.Title}\" của bạn đã được duyệt và xuất bản",
                        $"/Review/Detail/{post.PostId}",
                        post.PostId, // ReferenceId = PostId
                        cancellationToken
                    );
                }
                else if (request.NewStatus.Value == PostStatus.Rejected)
                {
                    var rejectionNote = !string.IsNullOrEmpty(request.ModerationNote) 
                        ? request.ModerationNote 
                        : "Bài viết không đáp ứng tiêu chuẩn của chúng tôi";
                    
                    await _notificationService.CreateNotificationAsync(
                        post.UserId,
                        "POST_REJECTED",
                        "Bài viết của bạn đã bị từ chối",
                        $"Bài viết \"{post.Title}\" của bạn đã bị từ chối. Lý do: {rejectionNote}",
                        $"/Review/MyReviews",
                        post.PostId, // ReferenceId = PostId
                        cancellationToken
                    );
                }
            }

            // Log action if status changed or delete/restore (chỉ log nếu có adminId hợp lệ)
            if ((request.NewStatus.HasValue || request.IsDeleted.HasValue) && adminId > 0)
            {
                var adminLog = new AdminLog
                {
                    AdminId = adminId,
                    ActionType = actionType,
                    TargetTable = "Posts",
                    TargetId = post.PostId,
                    Note = note,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AdminLogs.Add(adminLog);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
        
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
