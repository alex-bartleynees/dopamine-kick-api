using Common.Abstractions.Messaging;

namespace Common.IntegrationEvents.Quests;

[IntegrationEventRoutingKey(MessagingConstants.QuestReminderCreatedKey)]
public record QuestReminderCreated(
    Guid MessageId,
    Guid ReminderId,
    Guid QuestId,
    Guid UserId,
    DateTimeOffset RemindAt,
    string TimeZone,
    string QuestTitle,
    string QuestEmoji) : IntegrationEvent;
