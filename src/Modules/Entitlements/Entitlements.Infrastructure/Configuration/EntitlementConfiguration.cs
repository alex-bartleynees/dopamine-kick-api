using Entitlements.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entitlements.Infrastructure.Configuration;

public class EntitlementConfiguration : IEntityTypeConfiguration<Entitlement>
{
    public void Configure(EntityTypeBuilder<Entitlement> builder)
    {
        builder.HasKey(e => new { e.ProductId, e.UserId });
        builder.Property(e => e.ProductId).HasMaxLength(100);
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.HasIndex(e => e.UserId);
    }
}
