using System.Text.Json.Serialization;
using SharedKernel.Abstractions;

namespace Quests.Domain.Entities;

public class QuestReminder : IAuditable
{
    public Guid Id { get; set; }

    public Guid QuestId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset RemindAt { get; set; }

    public string TimeZone { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonIgnore]
    public Quest Quest { get; set; } = null!;
}
