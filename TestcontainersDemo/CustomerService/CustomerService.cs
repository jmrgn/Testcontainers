using CustomerService.ResourceAccess.DataAccess;
using CustomerService.ResourceAccess.Models;

namespace CustomerService;


public interface ICustomerServiceManager
{
    Task<Review> AddReviewAsync(Review review, CancellationToken cancellationToken = default);
    Task<Review?> GetReviewAsync(long reviewId, CancellationToken cancellationToken = default);
}

public class CustomerServiceManager : ICustomerServiceManager
{
    private readonly ReviewHandler _reviewHandler;
    private readonly CustomerHandler _customerHandler;

    public CustomerServiceManager(ReviewHandler reviewHandler, CustomerHandler customerHandler)
    {
        _reviewHandler = reviewHandler;
        _customerHandler = customerHandler;
    }

    public async Task<Review> AddReviewAsync(Review review, CancellationToken cancellationToken = default)
    {
        // Validate that the customer exists
        var customer = await _customerHandler.GetCustomerAsync(review.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {review.CustomerId} not found");
        }

        return await _reviewHandler.AddReviewAsync(review, cancellationToken);
    }

    public async Task<Review?> GetReviewAsync(long reviewId, CancellationToken cancellationToken = default)
    {
        return await _reviewHandler.GetReviewAsync(reviewId, cancellationToken);
    }
}

