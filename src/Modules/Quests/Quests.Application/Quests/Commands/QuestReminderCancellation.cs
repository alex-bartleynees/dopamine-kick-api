using System.Text.Json;
using Common.IntegrationEvents.Quests;
using Quests.Domain.Entities;

namespace Quests.Application.Quests.Commands;

/// <summary>
/// Builds the outbox message that tells the Notifications module to unschedule a
/// quest reminder's pending push notification (used on quest completion / deletion).
/// </summary>
internal static class QuestReminderCancellation
{
    public static OutboxMessage ToOutboxMessage(QuestReminder reminder)
    {
        var messageId = Guid.NewGuid();
        return new OutboxMessage
        {
            MessageId = messageId,
            Type = typeof(QuestReminderCancelled).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new QuestReminderCancelled(
                messageId,
                reminder.Id,
                reminder.UserId)),
            Published = false
        };
    }
}
