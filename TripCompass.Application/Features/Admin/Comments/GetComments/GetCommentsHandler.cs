using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common.Models;
using TripCompass.Application.DTOs;
using TripCompass.Application.Interfaces;

namespace TripCompass.Application.Features.Admin.Comments.GetComments
{
    public class GetCommentsHandler : IRequestHandler<GetCommentsQuery, PaginatedList<CommentListItemDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetCommentsHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<CommentListItemDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
        {
            var query = from comment in _context.PostComments
                       join post in _context.Posts on comment.PostId equals post.PostId
                       join user in _context.Users on comment.UserId equals user.UserId
                       select new { comment, post, user };

            // Filter by Search Term
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var term = request.SearchTerm.ToLower();
                query = query.Where(x =>
                    x.comment.Content.ToLower().Contains(term) ||
                    x.post.Title.ToLower().Contains(term) ||
                    x.user.UserName.ToLower().Contains(term) ||
                    x.user.Email.ToLower().Contains(term));
            }

            // Filter by PostId
            if (request.PostId.HasValue)
            {
                query = query.Where(x => x.comment.PostId == request.PostId.Value);
            }

            // Filter by UserId
            if (request.UserId.HasValue)
            {
                query = query.Where(x => x.comment.UserId == request.UserId.Value);
            }

            // Filter by IsDeleted
            if (request.IsDeleted.HasValue)
            {
                query = query.Where(x => x.comment.IsDeleted == request.IsDeleted.Value);
            }

            // Filter by Date
            if (request.FromDate.HasValue)
            {
                query = query.Where(x => x.comment.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(x => x.comment.CreatedAt <= request.ToDate.Value);
            }

            // Order By (Default: Newest first)
            query = query.OrderByDescending(x => x.comment.CreatedAt);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Get comments with pagination
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => x.comment)
                .ToListAsync(cancellationToken);

            // Get post and user info
            var postIds = items.Select(c => c.PostId).Distinct().ToList();
            var userIds = items.Select(c => c.UserId).Distinct().ToList();

            var posts = await _context.Posts
                .Where(p => postIds.Contains(p.PostId))
                .Select(p => new { p.PostId, p.Title })
                .ToListAsync(cancellationToken);

            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.UserName, u.Email })
                .ToListAsync(cancellationToken);

            // Get reaction counts
            var commentIds = items.Select(c => c.Id).ToList();
            var reactionCounts = await _context.CommentReactions
                .Where(r => commentIds.Contains(r.CommentId))
                .GroupBy(r => r.CommentId)
                .Select(g => new { CommentId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            // Project to DTOs
            var commentDtos = items.Select(c =>
            {
                var post = posts.FirstOrDefault(p => p.PostId == c.PostId);
                var user = users.FirstOrDefault(u => u.UserId == c.UserId);
                var reactionCount = reactionCounts.FirstOrDefault(r => r.CommentId == c.Id)?.Count ?? 0;
                
                return new CommentListItemDto
                {
                    CommentId = c.Id,
                    PostId = c.PostId,
                    PostTitle = post?.Title ?? "Unknown",
                    UserId = c.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    UserEmail = user?.Email ?? "Unknown",
                    ParentCommentId = c.ParentCommentId,
                    Content = c.Content,
                    IsDeleted = c.IsDeleted,
                    CreatedAt = c.CreatedAt,
                    ReactionCount = reactionCount
                };
            }).ToList();

            return new PaginatedList<CommentListItemDto>(
                commentDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);
        }
    }
}
