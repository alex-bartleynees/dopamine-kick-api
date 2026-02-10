namespace Habits.Application.Common.Models;

public record HabitReminderForCreationDto(
    Guid HabitId,
    TimeOnly NotificationTime,
    string Timezone,
    string PreferredTime,
    bool IsEnabled);