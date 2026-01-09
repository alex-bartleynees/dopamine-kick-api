using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Configuration;

public class WebPushSubscriptionConfiguration : IEntityTypeConfiguration<WebPushSubscription>
{
    public void Configure(EntityTypeBuilder<WebPushSubscription> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Endpoint).IsUnique();
        builder.HasIndex(x => x.IsActive);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Endpoint)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.P256dh)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Auth)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.FailureCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
