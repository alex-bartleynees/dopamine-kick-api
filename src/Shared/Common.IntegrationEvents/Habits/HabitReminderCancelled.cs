using SharedKernel.Messaging.Abstractions;

namespace Common.IntegrationEvents.Habits;

[IntegrationEventRoutingKey(RoutingKeys.HabitReminderCancelledKey)]
public record HabitReminderCancelled(
    Guid MessageId,
    Guid ReminderId,
    Guid UserId) : IntegrationEvent;
