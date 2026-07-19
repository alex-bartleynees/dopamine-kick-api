using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payments.Domain.Entities;

namespace Payments.Infrastructure.Configuration;

public class CustomerMappingConfiguration : IEntityTypeConfiguration<CustomerMapping>
{
    public void Configure(EntityTypeBuilder<CustomerMapping> builder)
    {
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.CustomerReference)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.CustomerReference)
            .IsUnique();
    }
}
