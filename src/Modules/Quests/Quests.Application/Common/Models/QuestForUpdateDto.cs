namespace Quests.Application.Common.Models;

public record QuestForUpdateDto(
    string Emoji,
    string Title,
    string? Description,
    DateTimeOffset DueAt);
