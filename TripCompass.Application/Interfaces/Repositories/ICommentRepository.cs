using TripCompass.Domain.Entities;

namespace TripCompass.Application.Interfaces.Repositories
{
    public interface ICommentRepository
    {
        // Create
        Task AddAsync(PostComment comment);

        // Read
        Task<PostComment?> GetByIdAsync(long id);

        Task<List<PostComment>> GetByPostAsync(long postId);

        // Update (soft delete / edit)
        void Update(PostComment comment);
    }
}
