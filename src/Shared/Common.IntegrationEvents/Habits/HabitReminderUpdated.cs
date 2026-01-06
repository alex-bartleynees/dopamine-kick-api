namespace Common.IntegrationEvents.Habits;

public record HabitReminderUpdated(
    Guid MessageId,
    Guid ReminderId,
    TimeOnly NotificationTime,
    string TimeZone);