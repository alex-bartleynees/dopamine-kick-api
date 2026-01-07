using Common.Abstractions.Messaging;
using Mediator;

namespace Common.IntegrationEvents.Habits;

[IntegrationEventRoutingKey(MessagingConstants.HabitReminderCreatedKey)]
public record HabitReminderCreated(
    Guid MessageId,
    Guid ReminderId,
    Guid UserId,
    TimeOnly NotificationTime,
    string TimeZone,
    string HabitName,
    string HabitEmoji,
    string HabitTarget) : IntegrationEvent, INotification;