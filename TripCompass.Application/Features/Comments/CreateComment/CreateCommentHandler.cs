using MediatR;
using TripCompass.Application.Features.Comments.CreateComment;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Application.Services;
using TripCompass.Domain.Entities;

namespace TripCompass.Application.Features.Comments.CreateComment
{
    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand>
    {
        private readonly ICommentRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly SentimentAnalysisService _sentimentService;

        public CreateCommentHandler(ICommentRepository repo, IUnitOfWork uow, SentimentAnalysisService sentimentService)
        {
            _repo = repo;
            _uow = uow;
            _sentimentService = sentimentService;
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
        await _uow.SaveChangesAsync();

        // Phân tích sentiment
        var sentiment = _sentimentService.Analyze(command.Content);

        // Tính coin và uy tín cho người comment (ít hơn người viết bài)
        // Người viết bài: 50-200 coin, người comment: 20-80 coin
        int coinEarned;
        int reputationEarned;

        if (sentiment.Sentiment == SentimentType.Positive)
        {
            // Tích cực: cộng coin và uy tín
            coinEarned = new Random().Next(20, 81); // 20-80 coin
            reputationEarned = new Random().Next(10, 41); // 10-40 điểm uy tín
        }
        else if (sentiment.Sentiment == SentimentType.Negative)
        {
            // Tiêu cực: trừ coin và uy tín (nhưng không quá nhiều)
            coinEarned = -new Random().Next(5, 21); // -5 đến -20 coin
            reputationEarned = -new Random().Next(5, 21); // -5 đến -20 điểm uy tín
        }
        else
        {
            // Neutral: ít coin và uy tín
            coinEarned = new Random().Next(5, 21); // 5-20 coin
            reputationEarned = new Random().Next(5, 16); // 5-15 điểm uy tín
        }

        // Cập nhật coin (nếu có wallet)
        var wallet = await _uow.Wallets.GetByUserIdAsync(command.UserId);
        if (wallet != null)
        {
            wallet.Balance = Math.Max(0, wallet.Balance + coinEarned); // Không cho âm
        }

        // Cập nhật uy tín
        user.ReputationScore = Math.Max(0, user.ReputationScore + reputationEarned);
        user.ReputationLevel = CalculateReputationLevel(user.ReputationScore);

        await _uow.SaveChangesAsync();
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
