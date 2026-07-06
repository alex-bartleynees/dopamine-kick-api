namespace Quests.Application.Common.Models;

public record QuestReminderForCreationDto(
    DateTimeOffset RemindAt,
    string TimeZone,
    bool IsEnabled = true);
