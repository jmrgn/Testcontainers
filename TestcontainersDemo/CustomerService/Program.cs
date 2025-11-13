using System.CommandLine;
using CustomerService.Commands;

namespace CustomerService;

public class Program
{
    private static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Customer Service command line interface.");

        var addReviewCommand = AddReviewCommand.Get();

        rootCommand.Add(addReviewCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
