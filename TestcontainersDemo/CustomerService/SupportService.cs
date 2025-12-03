using CustomerService.ResourceAccess.DataAccess;
using CustomerService.ResourceAccess.Models;

namespace CustomerService;

public interface ISupportServiceManager
{
    Task<Comment> AddCommentAsync(long reviewId, string commentText, string createdBy, CancellationToken cancellationToken = default);
    Task<Comment?> GetCommentAsync(long commentId, CancellationToken cancellationToken = default);
    Task<List<Comment>> GetCommentsByReviewIdAsync(long reviewId, CancellationToken cancellationToken = default);
    Task<Comment> UpdateCommentAsync(long commentId, string newCommentText, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(long commentId, CancellationToken cancellationToken = default);
    Task<List<Comment>> AddMultipleCommentsAsync(long reviewId, List<(string commentText, string createdBy)> comments, CancellationToken cancellationToken = default);
}

public class SupportServiceManager : ISupportServiceManager
{
    private readonly CommentHandler _commentHandler;
    private readonly ReviewHandler _reviewHandler;

    public SupportServiceManager(CommentHandler commentHandler, ReviewHandler reviewHandler)
    {
        _commentHandler = commentHandler;
        _reviewHandler = reviewHandler;
    }

    public async Task<Comment> AddCommentAsync(
        long reviewId,
        string commentText,
        string createdBy,
        CancellationToken cancellationToken = default
    )
    {
        // Validate that the review exists
        var review = await _reviewHandler.GetReviewAsync(reviewId, cancellationToken);
        if (review == null)
        {
            throw new InvalidOperationException($"Review with ID {reviewId} not found");
        }

        var comment = new Comment
        {
            ReviewId = review.Id,
            Review = review,
            CommentText = commentText,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow
        };

        return await _commentHandler.AddCommentAsync(comment, cancellationToken);
    }

    public async Task<Comment?> GetCommentAsync(long commentId, CancellationToken cancellationToken = default)
    {
        return await _commentHandler.GetCommentAsync(commentId, cancellationToken);
    }

    public async Task<List<Comment>> GetCommentsByReviewIdAsync(long reviewId, CancellationToken cancellationToken = default)
    {
        return await _commentHandler.GetCommentsByReviewIdAsync(reviewId, cancellationToken);
    }

    public async Task<Comment> UpdateCommentAsync(
        long commentId,
        string newCommentText,
        CancellationToken cancellationToken = default
    )
    {
        var existingComment = await _commentHandler.GetCommentAsync(commentId, cancellationToken);
        if (existingComment == null)
        {
            throw new InvalidOperationException($"Comment with ID {commentId} not found");
        }

        existingComment.CommentText = newCommentText;
        return await _commentHandler.UpdateCommentAsync(existingComment, cancellationToken);
    }

    public async Task<bool> DeleteCommentAsync(long commentId, CancellationToken cancellationToken = default)
    {
        return await _commentHandler.DeleteCommentAsync(commentId, cancellationToken);
    }

    public async Task<List<Comment>> AddMultipleCommentsAsync(
        long reviewId,
        List<(string commentText, string createdBy)> comments,
        CancellationToken cancellationToken = default
    )
    {
        // Validate that the review exists
        var review = await _reviewHandler.GetReviewAsync(reviewId, cancellationToken);
        if (review == null)
        {
            throw new InvalidOperationException($"Review with ID {reviewId} not found");
        }

        var addedComments = new List<Comment>();
        foreach (var (commentText, createdBy) in comments)
        {
            var comment = new Comment
            {
                ReviewId = reviewId,
                Review = review,
                CommentText = commentText,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };

            var addedComment = await _commentHandler.AddCommentAsync(comment, cancellationToken);
            addedComments.Add(addedComment);
        }

        return addedComments;
    }
}
