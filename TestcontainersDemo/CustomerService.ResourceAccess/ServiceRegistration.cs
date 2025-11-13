using CustomerService.ResourceAccess;
using CustomerService.ResourceAccess.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace ResourceAccess;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterDataAccess(
        this IServiceCollection services,
        IHostEnvironment environment,
        Action<DbContextOptionsBuilder> configure
    )
    {
        services
            .AddDbContext<CustomerServiceDBContext>(configure)
            .AddScoped<CustomerHandler>()
            .AddScoped<ReviewHandler>();

        return services;
    }
}
