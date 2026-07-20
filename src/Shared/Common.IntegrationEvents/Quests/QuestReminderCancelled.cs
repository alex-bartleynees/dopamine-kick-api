using SharedKernel.Messaging.Abstractions;

namespace Common.IntegrationEvents.Quests;

[IntegrationEventRoutingKey(RoutingKeys.QuestReminderCancelledKey)]
public record QuestReminderCancelled(
    Guid MessageId,
    Guid ReminderId,
    Guid UserId) : IntegrationEvent;
