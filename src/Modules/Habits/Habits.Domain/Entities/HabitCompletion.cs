namespace Habits.Domain.Entities;

public class HabitCompletion
{
   public Guid Id { get; set; }

   public Guid HabitId { get; set; }

   public DateOnly CompletedDate { get; set; }

   public DateTimeOffset CompletedAt { get; set; }

   public Habit Habit { get; set; } = null!;
}