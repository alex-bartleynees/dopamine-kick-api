using SharedKernel.Results;

namespace Habits.Domain.Errors;

/// <summary>
/// All domain errors a <see cref="Entities.HabitReminder"/> can produce, in one place.
/// </summary>
public static class HabitReminderErrors
{
    public static Error NotFound(Guid reminderId) =>
        Error.NotFound("HabitReminders.NotFound", $"Reminder with id {reminderId} was not found");
}
