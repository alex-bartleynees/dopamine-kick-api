using Habits.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Habits.Infrastructure.Configuration;

public class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.UserId)
            .IsRequired();

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Emoji)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(h => h.Target)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.CurrentStreak)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(h => h.LongestStreak)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(h => h.LastCompletedDate);

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        builder.Property(h => h.UpdatedAt)
            .IsRequired();

        builder.HasIndex(h => h.UserId);
    }
}