using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.ResourceAccess.DataAccess;

public class CommentHandler
{
    private readonly CustomerServiceDBContext _dbContext;

    public CommentHandler(CustomerServiceDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Comment> AddCommentAsync(
        Comment comment,
        CancellationToken cancellationToken = default
    )
    {
        _ = await _dbContext.Comments.AddAsync(comment, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task<Comment?> GetCommentAsync(
        long commentId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.Comments
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
    }

    public async Task<List<Comment>> GetCommentsByReviewIdAsync(
        long reviewId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext
            .Comments
            .Where(c => c.ReviewId == reviewId)
            .OrderBy(c => c.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Comment> UpdateCommentAsync(
        Comment comment,
        CancellationToken cancellationToken = default
    )
    {
        _ = _dbContext.Comments.Update(comment);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext
            .Comments.Include(c => c.Review)
            .Where(c => c.Id == comment.Id)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> DeleteCommentAsync(
        long commentId,
        CancellationToken cancellationToken = default
    )
    {
        var comment =
            await GetCommentAsync(commentId, cancellationToken)
            ?? throw new InvalidOperationException($"Comment with ID {commentId} not found");

        _dbContext.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
