using Common.Abstractions.Results;

namespace Quests.Domain.Errors;

/// <summary>
/// All domain errors a <see cref="Entities.QuestReminder"/> can produce, in one place.
/// </summary>
public static class QuestReminderErrors
{
    public static Error QuestCompleted() =>
        Error.Validation(
            "QuestReminders.QuestCompleted",
            "Cannot add a reminder to a completed quest");
}
