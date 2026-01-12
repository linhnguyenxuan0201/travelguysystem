using TripCompass.Application.Features.Comments.CreateComment;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;

public class CreateCommentHandler
{
    private readonly ICommentRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateCommentHandler(ICommentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(CreateCommentCommand command)
    {
        var user = await _uow.Users.GetByIdAsync(command.UserId);
        if (user == null) throw new Exception("User not found");
        if (user.IsBanned) throw new UnauthorizedAccessException("User is banned and cannot comment.");

        var comment = PostComment.Create(
            command.PostId,
            command.UserId,
            command.Content
        );

        await _repo.AddAsync(comment);
        await _uow.SaveChangesAsync();
    }
}
