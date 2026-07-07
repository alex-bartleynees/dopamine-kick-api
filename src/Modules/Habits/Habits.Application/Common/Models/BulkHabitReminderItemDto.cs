namespace Habits.Application.Common.Models;

public record BulkHabitReminderItemDto(
    Guid HabitId,
    TimeOnly NotificationTime,
    string TimeZone,
    string PreferredTime,
    bool IsEnabled);
