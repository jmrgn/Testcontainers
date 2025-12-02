using CustomerService.ResourceAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerService.ResourceAccess.EntityTypeConfigurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.ToTable("Comments").HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("CommentId");
            builder.Property(x => x.ReviewId).HasColumnName("ReviewId");
            builder.Property(x => x.CommentText).HasColumnName("CommentText").IsRequired();
            builder.Property(x => x.CreatedDate).HasColumnName("CreatedDate").IsRequired();
            builder.Property(x => x.CreatedBy).HasColumnName("CreatedBy").IsRequired();

            builder
                .HasOne(x => x.Review)
                .WithMany(r => r.Comments)
                .HasForeignKey(x => x.ReviewId);
        }
    }
}
