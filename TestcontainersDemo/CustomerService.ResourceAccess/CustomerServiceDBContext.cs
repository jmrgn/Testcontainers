using System;
using System.Collections.Generic;
using System.Text;
using CustomerService.ResourceAccess.EntityTypeConfigurations;
using CustomerService.ResourceAccess.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.ResourceAccess;

public class CustomerServiceDBContext : DbContext, IDataProtectionKeyContext
{
    public CustomerServiceDBContext(DbContextOptions<CustomerServiceDBContext> options)
        : base(options) { }

    public virtual DbSet<Customer> Customers => Set<Customer>();
    public virtual DbSet<Review> Reviews => Set<Review>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    internal IQueryable<Customer> RebateSettingsQuery => Customers;
    internal IQueryable<Review> ReviewQuery => Reviews;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity type configurations
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewConfiguration());
    }
}
