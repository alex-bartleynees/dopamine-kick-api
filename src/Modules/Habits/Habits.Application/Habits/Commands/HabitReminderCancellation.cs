using System.Text.Json;
using Common.IntegrationEvents.Habits;
using Habits.Domain.Entities;

namespace Habits.Application.Habits.Commands;

/// <summary>
/// Builds the outbox message that tells the Notifications module to unschedule a
/// habit reminder's recurring push notification (used on reminder disable / removal /
/// habit deletion).
/// </summary>
internal static class HabitReminderCancellation
{
    public static OutboxMessage ToOutboxMessage(HabitReminder reminder)
    {
        var messageId = Guid.NewGuid();
        return new OutboxMessage
        {
            MessageId = messageId,
            Type = typeof(HabitReminderCancelled).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new HabitReminderCancelled(
                messageId,
                reminder.Id,
                reminder.UserId)),
            Published = false
        };
    }
}
