using SharedKernel.Messaging.Abstractions;

namespace Common.IntegrationEvents.Habits;

[IntegrationEventRoutingKey(RoutingKeys.HabitReminderCreatedKey)]
public record HabitReminderCreated(
    Guid MessageId,
    Guid ReminderId,
    Guid UserId,
    TimeOnly NotificationTime,
    string TimeZone,
    string HabitName,
    string HabitEmoji,
    string HabitTarget) : IntegrationEvent;