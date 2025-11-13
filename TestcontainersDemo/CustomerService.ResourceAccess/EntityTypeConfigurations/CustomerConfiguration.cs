using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerService.ResourceAccess.EntityTypeConfigurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers").HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("CustomerId");
            builder.Property(x => x.Name).HasColumnName("Name").IsRequired();
        }
    }
}
