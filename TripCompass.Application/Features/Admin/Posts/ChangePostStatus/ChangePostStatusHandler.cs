using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Auth;
using TripCompass.Application.Common;
using TripCompass.Application.Interfaces;
using TripCompass.Domain.Entities;
using TripCompass.Domain.Enums;

namespace TripCompass.Application.Features.Admin.Posts.ChangePostStatus
{
    public class ChangePostStatusHandler : IRequestHandler<ChangePostStatusCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ChangePostStatusHandler(IApplicationDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
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
    }
}
