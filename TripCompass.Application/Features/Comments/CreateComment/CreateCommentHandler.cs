using MediatR;
using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Common;
using TripCompass.Application.Features.Comments.CreateComment;
using TripCompass.Application.Interfaces;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Application.Services;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Comments.CreateComment
{
    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand>
    {
        private readonly ICommentRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IApplicationDbContext _context;
        private readonly SentimentAnalysisService _sentimentService;
        private readonly TripCompass.Application.Services.NotificationService _notificationService;

        public CreateCommentHandler(
            ICommentRepository repo, 
            IUnitOfWork uow, 
            IApplicationDbContext context,
            SentimentAnalysisService sentimentService,
            TripCompass.Application.Services.NotificationService notificationService)
        {
            _repo = repo;
            _uow = uow;
            _context = context;
            _sentimentService = sentimentService;
            _notificationService = notificationService;
        }

        public async Task Handle(CreateCommentCommand command, CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByIdAsync(command.UserId);
        if (user == null) throw new Exception("User not found");
        if (user.IsBanned) throw new UnauthorizedAccessException("User is banned and cannot comment.");

        var comment = PostComment.Create(
            command.PostId,
            command.UserId,
            command.Content,
            command.ParentCommentId
        );

        await _repo.AddAsync(comment);
        await _uow.SaveChangesAsync(cancellationToken);

        // Phân tích sentiment
        var sentiment = _sentimentService.Analyze(command.Content);

        // Tính coin (ReputationScore) cho người comment (ít hơn người viết bài)
        // Người viết bài: 50-200 coin, người comment: 20-80 coin
        // Lưu ý: Coin = ReputationScore (điểm thưởng ảo), Wallet = tiền thật (VND)
        int coinEarned;

        if (sentiment.Sentiment == SentimentType.Positive)
        {
            // Tích cực: cộng coin (20-80 coin)
            coinEarned = new Random().Next(20, 81);
        }
        else if (sentiment.Sentiment == SentimentType.Negative)
        {
            // Tiêu cực: trừ coin (nhưng không quá nhiều) (-5 đến -20 coin)
            coinEarned = -new Random().Next(5, 21);
        }
        else
        {
            // Neutral: ít coin (5-20 coin)
            coinEarned = new Random().Next(5, 21);
        }

        // Cập nhật coin (ReputationScore) - Coin là điểm thưởng ảo, KHÔNG phải tiền thật
        user.ReputationScore = Math.Max(0, user.ReputationScore + coinEarned);
        user.ReputationLevel = CalculateReputationLevel(user.ReputationScore);

        await _uow.SaveChangesAsync(cancellationToken);

        // Gửi thông báo cho tác giả bài viết (nếu không phải là chính họ)
        try
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PostId == command.PostId, cancellationToken);

            if (post != null && post.UserId != command.UserId)
            {
                await _notificationService.NotifyNewCommentAsync(
                    post.UserId,
                    command.PostId,
                    comment.Id,
                    user.UserName
                );
            }
        }
        catch
        {
            // Ignore notification errors
        }

        // Log activity
        await ActivityLogger.LogActivityAsync(
            _context,
            command.UserId,
            "CREATE_COMMENT",
            "PostComments",
            comment.Id,
            $"Commented on post ID: {command.PostId}. Sentiment: {sentiment.Sentiment}");
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
