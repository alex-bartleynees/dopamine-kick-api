namespace Common.IntegrationEvents.Habits;

public record HabitReminderCreated(
    Guid MessageId,
    Guid ReminderId,
    Guid UserId,
    TimeOnly NotificationTime,
    string TimeZone);