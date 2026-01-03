using Habits.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Habits.Infrastructure.Configuration;

public class HabitReminderConfiguration : IEntityTypeConfiguration<HabitReminder>
{
    public void Configure(EntityTypeBuilder<HabitReminder> builder)
    {
        builder.HasKey(hr => hr.Id);

        builder.Property(hr => hr.UserId)
            .IsRequired();

        builder.Property(hr => hr.HabitId)
            .IsRequired();

        builder.Property(hr => hr.NotificationTime)
            .IsRequired();

        builder.Property(hr => hr.TimeZone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(hr => hr.PreferredTime)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(hr => hr.IsEnabled)
            .IsRequired();

        builder.Property(hr => hr.CreatedAt)
            .IsRequired();

        builder.Property(hr => hr.UpdatedAt)
            .IsRequired();

        builder.HasOne(hr => hr.Habit)
            .WithMany()
            .HasForeignKey(hr => hr.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(hr => hr.UserId);

        builder.HasIndex(hr => hr.HabitId)
            .IsUnique();
    }
}
