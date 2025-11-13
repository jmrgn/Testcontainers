using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.ResourceAccess.DataAccess;

public class CustomerHandler
{
    private readonly CustomerServiceDBContext _dbContext;

    public CustomerHandler(CustomerServiceDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Customer> AddCustomerAsync(
        Customer customer,
        CancellationToken cancellationToken = default
    )
    {
        _ = await _dbContext.Customers.AddAsync(customer, cancellationToken);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer?> GetCustomerAsync(
        long customerId,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.Customers.FirstOrDefaultAsync(
            c => c.Id == customerId,
            cancellationToken
        );
    }

    public async Task<List<Customer>> GetCustomersAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.Customers.ToListAsync(cancellationToken);
    }

    public async Task<List<Customer>> GetCustomersByIdsAsync(
        IEnumerable<long> customerIds,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext
            .Customers.Where(c => customerIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer> UpdateCustomerAsync(
        Customer customer,
        CancellationToken cancellationToken = default
    )
    {
        _ = _dbContext.Customers.Update(customer);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext
            .Customers.Where(c => c.Id == customer.Id)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> DeleteCustomerAsync(
        long customerId,
        CancellationToken cancellationToken = default
    )
    {
        var customer =
            await GetCustomerAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        _dbContext.Remove(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
