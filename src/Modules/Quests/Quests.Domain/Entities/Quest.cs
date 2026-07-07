using Common.Abstractions;

namespace Quests.Domain.Entities;

public class Quest : IAuditable
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Emoji { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset DueAt { get; set; }

    public QuestStatus Status { get; set; } = QuestStatus.Pending;

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<QuestReminder> Reminders { get; set; } = [];
}
