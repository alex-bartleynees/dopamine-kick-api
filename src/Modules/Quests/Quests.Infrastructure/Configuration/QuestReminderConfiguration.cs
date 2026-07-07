using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quests.Domain.Entities;

namespace Quests.Infrastructure.Configuration;

public class QuestReminderConfiguration : IEntityTypeConfiguration<QuestReminder>
{
    public void Configure(EntityTypeBuilder<QuestReminder> builder)
    {
        builder.HasKey(qr => qr.Id);

        builder.Property(qr => qr.QuestId)
            .IsRequired();

        builder.Property(qr => qr.UserId)
            .IsRequired();

        builder.Property(qr => qr.RemindAt)
            .IsRequired();

        builder.Property(qr => qr.TimeZone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(qr => qr.IsEnabled)
            .IsRequired();

        builder.Property(qr => qr.CreatedAt)
            .IsRequired();

        builder.Property(qr => qr.UpdatedAt)
            .IsRequired();

        builder.HasIndex(qr => qr.UserId);
        builder.HasIndex(qr => qr.QuestId);
    }
}
