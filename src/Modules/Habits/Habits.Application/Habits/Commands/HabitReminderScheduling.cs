using System.Text.Json;
using Common.IntegrationEvents.Habits;
using Habits.Domain.Entities;

namespace Habits.Application.Habits.Commands;

/// <summary>
/// Builds the outbox message that tells the Notifications module to schedule (or
/// reschedule) a habit reminder's recurring push notification. The scheduler is
/// idempotent by reminder id, so re-emitting this after a time/text change reschedules.
/// </summary>
internal static class HabitReminderScheduling
{
    public static OutboxMessage ToOutboxMessage(HabitReminder reminder, Habit habit)
    {
        var messageId = Guid.NewGuid();
        return new OutboxMessage
        {
            MessageId = messageId,
            Type = typeof(HabitReminderCreated).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new HabitReminderCreated(
                messageId,
                reminder.Id,
                reminder.UserId,
                reminder.NotificationTime,
                reminder.TimeZone,
                habit.Name,
                habit.Emoji,
                habit.Target)),
            Published = false
        };
    }
}
