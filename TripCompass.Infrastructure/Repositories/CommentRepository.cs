using Microsoft.EntityFrameworkCore;
using TripCompass.Application.Interfaces.Repositories;
using TripCompass.Domain.Entities;
using TripCompass.Infrastructure.Persistence;

namespace TripCompass.Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly AppDbContext _db;

        public CommentRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(PostComment comment)
        {
            await _db.PostComments.AddAsync(comment);
        }

        public async Task<PostComment?> GetByIdAsync(long id)
        {
            return await _db.PostComments
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        }

        public async Task<List<PostComment>> GetByPostAsync(long postId)
        {
            return await _db.PostComments
                .Where(c => c.PostId == postId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public void Update(PostComment comment)
        {
            _db.PostComments.Update(comment);
        }
    }
}
