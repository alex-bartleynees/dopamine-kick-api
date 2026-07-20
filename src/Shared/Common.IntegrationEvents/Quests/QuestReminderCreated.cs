using SharedKernel.Messaging.Abstractions;

namespace Common.IntegrationEvents.Quests;

[IntegrationEventRoutingKey(RoutingKeys.QuestReminderCreatedKey)]
public record QuestReminderCreated(
    Guid MessageId,
    Guid ReminderId,
    Guid QuestId,
    Guid UserId,
    DateTimeOffset RemindAt,
    string TimeZone,
    string QuestTitle,
    string QuestEmoji) : IntegrationEvent;
