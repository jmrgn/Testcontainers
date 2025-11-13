using System.CommandLine;
using CustomerService.ResourceAccess;
using CustomerService.ResourceAccess.DataAccess;
using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Commands
{
    public static class AddReviewCommand
    {
        public static Command Get()
        {
            var customerIdOption = new Option<long>(
                "--customerId",
                description: "The ID of the customer for the review."
            )
            {
                IsRequired = true
            };
            customerIdOption.AddAlias("-c");

            var ratingOption = new Option<ReviewRating>(
                "--rating",
                description: "The rating for the review (Poor, Fair, Good, VeryGood, Excellent)."
            )
            {
                IsRequired = true
            };
            ratingOption.AddAlias("-r");

            var commentOption = new Option<string>(
                "--comment",
                description: "The comment text for the review."
            )
            {
                IsRequired = true
            };
            commentOption.AddAlias("-m");

            var connectionStringOption = new Option<string>(
                "--connectionString",
                description: "The connection string to connect to the database."
            )
            {
                IsRequired = true
            };
            connectionStringOption.AddAlias("-cs");

            var addReviewCommand = new Command(
                "add-review",
                "Adds a new review for a customer."
            )
            {
                customerIdOption,
                ratingOption,
                commentOption,
                connectionStringOption
            };

            addReviewCommand.SetHandler(
                async (customerId, rating, comment, connectionString) =>
                {
                    try
                    {
                        Console.WriteLine("Adding review...");
                        Console.WriteLine($"Customer ID: {customerId}");
                        Console.WriteLine($"Rating: {rating}");
                        Console.WriteLine($"Comment: {comment}");

                        var options = new DbContextOptionsBuilder<CustomerServiceDBContext>();
                        options.UseSqlServer(connectionString);
                        options.LogTo(Console.WriteLine);

                        await using var context = new CustomerServiceDBContext(options.Options);

                        // Verify customer exists
                        var customer = await context.Customers.FindAsync(customerId);
                        if (customer == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Error: Customer with ID {customerId} not found.");
                            Console.ResetColor();
                            return;
                        }

                        var reviewHandler = new ReviewHandler(context);
                        var customerHandler = new CustomerHandler(context);
                        var customerService = new CustomerServiceManager(reviewHandler, customerHandler);

                        var review = new Review
                        {
                            CustomerId = customerId,
                            Rating = rating,
                            Comments = comment
                        };

                        var addedReview = await customerService.AddReviewAsync(review);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Review added successfully!");
                        Console.WriteLine($"  Review ID: {addedReview.Id}");
                        Console.WriteLine($"  Customer: {customer.Name}");
                        Console.WriteLine($"  Rating: {addedReview.Rating}");
                        Console.WriteLine($"  Comment: {addedReview.Comments}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                        Console.ResetColor();
                    }
                },
                customerIdOption,
                ratingOption,
                commentOption,
                connectionStringOption
            );

            return addReviewCommand;
        }
    }
}
