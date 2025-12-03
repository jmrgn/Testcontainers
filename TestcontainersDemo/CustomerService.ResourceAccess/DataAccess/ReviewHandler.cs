using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.ResourceAccess.DataAccess;

public class ReviewHandler
{
    private readonly CustomerServiceDBContext _dbContext;

    public ReviewHandler(CustomerServiceDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Review> AddReviewAsync(
        Review review,
        CancellationToken cancellationToken = default
    )
    {
        _ = await _dbContext.Reviews.AddAsync(review, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task<Review?> GetReviewAsync(
        long reviewId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
    }

    public async Task<List<Review>> GetReviewsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Comments)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Review>> GetReviewsByCustomerIdAsync(
        long customerId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext
            .Reviews.Include(r => r.Customer)
            .Include(r => r.Comments)
            .Where(r => r.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Review>> GetReviewsByRatingAsync(
        ReviewRating rating,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext
            .Reviews.Include(r => r.Customer)
            .Include(r => r.Comments)
            .Where(r => r.Rating == rating)
            .ToListAsync(cancellationToken);
    }

    public async Task<Review> UpdateReviewAsync(
        Review review,
        CancellationToken cancellationToken = default
    )
    {
        _ = _dbContext.Reviews.Update(review);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext
            .Reviews.Include(r => r.Customer)
            .Include(r => r.Comments)
            .Where(r => r.Id == review.Id)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> DeleteReviewAsync(
        long reviewId,
        CancellationToken cancellationToken = default
    )
    {
        var review =
            await GetReviewAsync(reviewId, cancellationToken)
            ?? throw new InvalidOperationException($"Review with ID {reviewId} not found");

        _dbContext.Remove(review);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
