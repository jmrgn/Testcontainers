using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerService.ResourceAccess.EntityTypeConfigurations
{
    public class ReviewConfiguration : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.ToTable("Reviews").HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("ReviewId");
            builder.Property(x => x.CustomerId).HasColumnName("CustomerId");
            builder.Property(x => x.Rating).HasColumnName("Rating").HasConversion<int>();

            builder
                .HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId);

            builder
                .HasMany(x => x.Comments)
                .WithOne(c => c.Review)
                .HasForeignKey(c => c.ReviewId);
        }
    }
}
