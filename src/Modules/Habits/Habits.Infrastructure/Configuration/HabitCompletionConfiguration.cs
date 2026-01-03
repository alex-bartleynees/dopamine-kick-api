using Habits.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Habits.Infrastructure.Configuration;

public class HabitCompletionConfiguration : IEntityTypeConfiguration<HabitCompletion>
{
    public void Configure(EntityTypeBuilder<HabitCompletion> builder)
    {
        builder.HasKey(hc => hc.Id);

        builder.Property(hc => hc.HabitId)
            .IsRequired();

        builder.Property(hc => hc.CompletedDate)
            .IsRequired();

        builder.Property(hc => hc.CompletedAt)
            .IsRequired();

        builder.HasOne(hc => hc.Habit)
            .WithMany()
            .HasForeignKey(hc => hc.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(hc => hc.HabitId);

        builder.HasIndex(hc => new { hc.HabitId, hc.CompletedDate })
            .IsUnique();
    }
}
