using Common.Abstractions.Messaging;

namespace Common.IntegrationEvents.Habits;

[IntegrationEventRoutingKey(MessagingConstants.HabitReminderCancelledKey)]
public record HabitReminderCancelled(
    Guid MessageId,
    Guid ReminderId,
    Guid UserId) : IntegrationEvent;
