using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quests.Domain.Entities;

namespace Quests.Infrastructure.Configuration;

public class QuestConfiguration : IEntityTypeConfiguration<Quest>
{
    public void Configure(EntityTypeBuilder<Quest> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.UserId)
            .IsRequired();

        builder.Property(q => q.Emoji)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(q => q.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(q => q.Description)
            .HasMaxLength(500);

        builder.Property(q => q.DueAt)
            .IsRequired();

        builder.Property(q => q.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(q => q.CompletedAt);

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        builder.Property(q => q.UpdatedAt)
            .IsRequired();

        builder.HasMany(q => q.Reminders)
            .WithOne(r => r.Quest)
            .HasForeignKey(r => r.QuestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => q.UserId);
        builder.HasIndex(q => new { q.UserId, q.Status });
    }
}
