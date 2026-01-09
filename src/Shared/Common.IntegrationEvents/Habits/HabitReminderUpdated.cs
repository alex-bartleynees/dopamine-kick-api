using Common.Abstractions.Messaging;
using Mediator;

namespace Common.IntegrationEvents.Habits;

[IntegrationEventRoutingKey(MessagingConstants.HabitReminderUpdatedKey)]
public record HabitReminderUpdated(
    Guid MessageId,
    Guid ReminderId,
    TimeOnly NotificationTime,
    string TimeZone) : IntegrationEvent;