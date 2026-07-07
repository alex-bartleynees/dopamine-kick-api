using Common.Abstractions.Messaging;

namespace Common.IntegrationEvents.Quests;

[IntegrationEventRoutingKey(MessagingConstants.QuestReminderCancelledKey)]
public record QuestReminderCancelled(
    Guid MessageId,
    Guid ReminderId,
    Guid UserId) : IntegrationEvent;
