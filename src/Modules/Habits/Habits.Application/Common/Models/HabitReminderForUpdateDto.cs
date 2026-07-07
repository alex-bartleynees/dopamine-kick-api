namespace Habits.Application.Common.Models;

public record HabitReminderForUpdateDto(
    TimeOnly NotificationTime,
    string TimeZone,
    string PreferredTime,
    bool IsEnabled);
