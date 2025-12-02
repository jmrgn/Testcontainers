using System;
using System.Data.Common;
using CustomerService.ResourceAccess;
using CustomerService.ResourceAccess.DataAccess;
using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace CustomerService.Tests;

public sealed class SupportMsSqlTests : IAsyncLifetime
{
    internal readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithName("TestContainerDB")
        .WithLabel("reuse-id", "TestContainerDB")
        .WithReuse(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }
}

/*
 * Experimental:
[CollectionDefinition("SupportService Collection", DisableParallelization = true)]
public class SupportServiceCollection : ICollectionFixture<SupportMsSqlTests> { }

[Collection("SupportService Collection")]
*/
public sealed class SupportServiceTests : IClassFixture<SupportMsSqlTests>, IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    private readonly CustomerServiceDBContext _dbContext;
    private readonly ICustomerServiceManager _customerServiceManager;
    private readonly ISupportServiceManager _supportServiceManager;

    public SupportServiceTests(SupportMsSqlTests fixture)
    {
        ServiceProvider = new CustomServiceFactory(fixture).ConfigureServices();

        try
        {
            _dbContext = ServiceProvider.GetRequiredService<CustomerServiceDBContext>();
            _customerServiceManager = ServiceProvider.GetRequiredService<ICustomerServiceManager>();
            _supportServiceManager = ServiceProvider.GetRequiredService<ISupportServiceManager>();

            // Create the database schema (tables, indexes, etc.)
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
            _dbContext.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    #region Setup

    private sealed class CustomServiceFactory
    {
        private readonly string _connectionString;

        public CustomServiceFactory(SupportMsSqlTests fixture)
        {
            var baseConnectionString = fixture._msSqlContainer.GetConnectionString();
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
                baseConnectionString
            )
            {
                InitialCatalog = "SupportServiceTestDb"
            };
            _connectionString = builder.ConnectionString;
        }

        public IServiceProvider ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Remove(
                serviceCollection.SingleOrDefault(service =>
                    typeof(DbContextOptions<CustomerServiceDBContext>) == service.ServiceType
                )
            );
            serviceCollection.Remove(
                serviceCollection.SingleOrDefault(service =>
                    typeof(DbConnection) == service.ServiceType
                )
            );

            serviceCollection
                .AddDbContext<CustomerServiceDBContext>(
                    (_, option) => option.UseSqlServer(_connectionString)
                )
                .AddScoped<CustomerHandler>()
                .AddScoped<ReviewHandler>()
                .AddScoped<CommentHandler>()
                .AddScoped<ICustomerServiceManager, CustomerServiceManager>()
                .AddScoped<ISupportServiceManager, SupportServiceManager>();
            return serviceCollection.BuildServiceProvider();
        }
    }

    #endregion

    #region Teardown

    public void Dispose()
    {
        var dbContext = ServiceProvider.GetRequiredService<CustomerServiceDBContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Dispose();
    }

    #endregion

    #region Helper Methods

    private async Task<Review> CreateTestReviewAsync()
    {
        var customer = new Customer { Name = "Test Customer" };
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var review = new Review { CustomerId = customer.Id, Rating = ReviewRating.Good };
        return await _customerServiceManager.AddReviewAsync(review, CancellationToken.None);
    }

    #endregion

    #region Add Comment Tests

    [Fact]
    public async Task AddCommentAsync_ShouldAddCommentSuccessfully()
    {
        // Arrange
        var review = await CreateTestReviewAsync();
        var commentText = "This is a test comment";
        var createdBy = "Test User";

        // Act
        var result = await _supportServiceManager.AddCommentAsync(
            review.Id,
            commentText,
            createdBy,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(commentText, result.CommentText);
        Assert.Equal(createdBy, result.CreatedBy);
        Assert.Equal(review.Id, result.ReviewId);
        Assert.True(result.Id > 0);
        Assert.True(result.CreatedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddCommentAsync_WithInvalidReviewId_ShouldThrowException()
    {
        // Arrange
        var invalidReviewId = 99999L;
        var commentText = "This should fail";
        var createdBy = "Test User";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await _supportServiceManager.AddCommentAsync(
                    invalidReviewId,
                    commentText,
                    createdBy,
                    CancellationToken.None
                )
        );
    }

    #endregion

    #region Get Comment Tests

    [Fact]
    public async Task GetCommentAsync_ShouldReturnComment()
    {
        // Arrange
        var review = await CreateTestReviewAsync();
        var commentText = "Test comment to retrieve";
        var addedComment = await _supportServiceManager.AddCommentAsync(
            review.Id,
            commentText,
            "Test User",
            CancellationToken.None
        );

        // Act
        var result = await _supportServiceManager.GetCommentAsync(
            addedComment.Id,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(addedComment.Id, result.Id);
        Assert.Equal(commentText, result.CommentText);
        Assert.NotNull(result.Review);
    }

    [Fact]
    public async Task GetCommentAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidCommentId = 99999L;

        // Act
        var result = await _supportServiceManager.GetCommentAsync(
            invalidCommentId,
            CancellationToken.None
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCommentsByReviewIdAsync_ShouldReturnAllComments()
    {
        // Arrange
        var review = await CreateTestReviewAsync();
        await _supportServiceManager.AddCommentAsync(
            review.Id,
            "First comment",
            "User1",
            CancellationToken.None
        );
        await _supportServiceManager.AddCommentAsync(
            review.Id,
            "Second comment",
            "User2",
            CancellationToken.None
        );
        await _supportServiceManager.AddCommentAsync(
            review.Id,
            "Third comment",
            "User3",
            CancellationToken.None
        );

        // Act
        var result = await _supportServiceManager.GetCommentsByReviewIdAsync(
            review.Id,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("First comment", result[0].CommentText);
        Assert.Equal("Second comment", result[1].CommentText);
        Assert.Equal("Third comment", result[2].CommentText);
    }

    [Fact]
    public async Task GetCommentsByReviewIdAsync_WithNoComments_ShouldReturnEmptyList()
    {
        // Arrange
        var review = await CreateTestReviewAsync();

        // Act
        var result = await _supportServiceManager.GetCommentsByReviewIdAsync(
            review.Id,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Update Comment Tests

    [Fact]
    public async Task UpdateCommentAsync_ShouldUpdateCommentText()
    {
        // Arrange
        var review = await CreateTestReviewAsync();
        var originalText = "Original comment text";
        var updatedText = "Updated comment text";
        var comment = await _supportServiceManager.AddCommentAsync(
            review.Id,
            originalText,
            "Test User",
            CancellationToken.None
        );

        // Act
        var result = await _supportServiceManager.UpdateCommentAsync(
            comment.Id,
            updatedText,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(comment.Id, result.Id);
        Assert.Equal(updatedText, result.CommentText);
        Assert.NotEqual(originalText, result.CommentText);
    }

    [Fact]
    public async Task UpdateCommentAsync_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var invalidCommentId = 99999L;
        var updatedText = "This should fail";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await _supportServiceManager.UpdateCommentAsync(
                    invalidCommentId,
                    updatedText,
                    CancellationToken.None
                )
        );
    }

    #endregion

    #region Delete Comment Tests

    [Fact]
    public async Task DeleteCommentAsync_ShouldDeleteComment()
    {
        // Arrange
        var review = await CreateTestReviewAsync();
        var comment = await _supportServiceManager.AddCommentAsync(
            review.Id,
            "Comment to delete",
            "Test User",
            CancellationToken.None
        );

        // Act
        var result = await _supportServiceManager.DeleteCommentAsync(
            comment.Id,
            CancellationToken.None
        );

        // Assert
        Assert.True(result);

        // Verify comment is deleted
        var deletedComment = await _supportServiceManager.GetCommentAsync(
            comment.Id,
            CancellationToken.None
        );
        Assert.Null(deletedComment);
    }

    [Fact]
    public async Task DeleteCommentAsync_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var invalidCommentId = 99999L;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await _supportServiceManager.DeleteCommentAsync(
                    invalidCommentId,
                    CancellationToken.None
                )
        );
    }

    #endregion

    #region Add Multiple Comments Tests

    [Fact]
    public async Task AddMultipleCommentsAsync_ShouldAddAllComments()
    {
        #region Data Setup

        var customer = new Customer
        {
            Name = "Test Customer AddMultipleCommentsAsync_ShouldAddAllComments",
        };
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        #endregion

        // Arrange
        var review = await CreateTestReviewAsync();
        var comments = new List<(string commentText, string createdBy)>
        {
            ("First comment", "User1"),
            ("Second comment", "User2"),
            ("Third comment", "User3")
        };

        // Act
        var result = await _supportServiceManager.AddMultipleCommentsAsync(
            review.Id,
            comments,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("First comment", result[0].CommentText);
        Assert.Equal("User1", result[0].CreatedBy);
        Assert.Equal("Second comment", result[1].CommentText);
        Assert.Equal("User2", result[1].CreatedBy);
        Assert.Equal("Third comment", result[2].CommentText);
        Assert.Equal("User3", result[2].CreatedBy);

        // Verify in database
        var commentsFromDb = await _supportServiceManager.GetCommentsByReviewIdAsync(
            review.Id,
            CancellationToken.None
        );
        Assert.Equal(3, commentsFromDb.Count);
    }

    [Fact]
    public async Task AddMultipleCommentsAsync_WithInvalidReviewId_ShouldThrowException()
    {
        // Arrange
        var invalidReviewId = 99999L;
        var comments = new List<(string commentText, string createdBy)>
        {
            ("This should fail", "User1")
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await _supportServiceManager.AddMultipleCommentsAsync(
                    invalidReviewId,
                    comments,
                    CancellationToken.None
                )
        );
    }

    [Fact]
    public async Task AddMultipleCommentsAsync_WithEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var review = await CreateTestReviewAsync();
        var comments = new List<(string commentText, string createdBy)>();

        // Act
        var result = await _supportServiceManager.AddMultipleCommentsAsync(
            review.Id,
            comments,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange - Create review
        var review = await CreateTestReviewAsync();

        // Act & Assert - Add multiple comments
        var initialComments = new List<(string commentText, string createdBy)>
        {
            ("Great product!", "Customer1"),
            ("Fast shipping", "Customer2")
        };
        var addedComments = await _supportServiceManager.AddMultipleCommentsAsync(
            review.Id,
            initialComments,
            CancellationToken.None
        );
        Assert.Equal(2, addedComments.Count);

        // Add another comment
        var additionalComment = await _supportServiceManager.AddCommentAsync(
            review.Id,
            "Excellent customer service",
            "Customer3",
            CancellationToken.None
        );
        Assert.NotNull(additionalComment);

        // Get all comments
        var allComments = await _supportServiceManager.GetCommentsByReviewIdAsync(
            review.Id,
            CancellationToken.None
        );
        Assert.Equal(3, allComments.Count);

        // Update a comment
        var updatedComment = await _supportServiceManager.UpdateCommentAsync(
            addedComments[0].Id,
            "Great product! Updated review",
            CancellationToken.None
        );
        Assert.Equal("Great product! Updated review", updatedComment.CommentText);

        // Delete a comment
        var deleteResult = await _supportServiceManager.DeleteCommentAsync(
            addedComments[1].Id,
            CancellationToken.None
        );
        Assert.True(deleteResult);

        // Verify final state
        var finalComments = await _supportServiceManager.GetCommentsByReviewIdAsync(
            review.Id,
            CancellationToken.None
        );
        Assert.Equal(2, finalComments.Count);

        // Verify the review includes comments
        var reviewFromDb = await _customerServiceManager.GetReviewAsync(
            review.Id,
            CancellationToken.None
        );
        Assert.NotNull(reviewFromDb);
        Assert.Equal(2, reviewFromDb.Comments.Count);
    }

    #endregion
}
