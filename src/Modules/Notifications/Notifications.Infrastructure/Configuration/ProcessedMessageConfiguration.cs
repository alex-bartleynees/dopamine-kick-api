using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Configuration;

public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(h => h.Id);

        builder.HasIndex(x => x.MessageId).IsUnique();
        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.MessageId)
            .IsRequired();

        builder.Property(x => x.MessageType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}