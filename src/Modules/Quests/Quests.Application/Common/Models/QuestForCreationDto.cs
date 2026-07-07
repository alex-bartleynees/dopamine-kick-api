namespace Quests.Application.Common.Models;

public record QuestForCreationDto(
    string Emoji,
    string Title,
    string? Description,
    DateTimeOffset DueAt);
