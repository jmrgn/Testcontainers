using System;
using System.Data.Common;
using System.Net.Http;
using CustomerService.ResourceAccess;
using CustomerService.ResourceAccess;
using CustomerService.ResourceAccess.DataAccess;
using CustomerService.ResourceAccess.DataAccess;
using CustomerService.ResourceAccess.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace CustomerService.Tests;

public sealed class MsSqlTests : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithName("CustomerReviewTestDB")
        .Build();

    public Task InitializeAsync()
    {
        return _msSqlContainer.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    public sealed class CustomerServiceTests : IClassFixture<MsSqlTests>, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }
        private readonly CustomerServiceDBContext _dbContext;
        private readonly ICustomerServiceManager _customerServiceManager;

        public CustomerServiceTests(MsSqlTests fixture)
        {
            ServiceProvider = new CustomServiceFactory(fixture).ConfigureServices();

            try
            {
                _dbContext = ServiceProvider.GetRequiredService<CustomerServiceDBContext>();
                _customerServiceManager =
                    ServiceProvider.GetRequiredService<ICustomerServiceManager>();

                // Create the database schema (tables, indexes, etc.)
                _dbContext.Database.EnsureCreated();
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

            public CustomServiceFactory(MsSqlTests fixture)
            {
                // Add explicit database name to avoid using 'master'
                // Add explicit database name to avoid using 'master'
                var baseConnectionString = fixture._msSqlContainer.GetConnectionString();
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnectionString)
                {
                    InitialCatalog = "CustomerServiceTestDb"
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
                    .AddScoped<ICustomerServiceManager, CustomerServiceManager>();
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

        #region Add Review Tests

        [Fact]
        public async Task AddReviewAsync_ShouldAddReviewSuccessfully()
        {
            #region Data Setup

            var customer = new Customer { Name = "Test Customer", };
            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            #endregion

            var review = new Review
            {
                CustomerId = customer.Id,
                Rating = ReviewRating.Excellent,
                Comments = "Build DAAAAAAAAAAAAAAY!!!"
            };
            var result = await _customerServiceManager.AddReviewAsync(
                review,
                CancellationToken.None
            );


            var actualFromDb = await _customerServiceManager.GetReviewAsync(
                result.Id,
                CancellationToken.None
            );
            Assert.NotNull(result);
            Assert.Equal(review.Comments, actualFromDb.Comments);
        }
        
        #endregion
    }
}
