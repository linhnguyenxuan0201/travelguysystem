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
        var comment = PostComment.Create(
            command.PostId,
            command.UserId,
            command.Content
        );

        await _repo.AddAsync(comment);
        await _uow.SaveChangesAsync();
    }
}
