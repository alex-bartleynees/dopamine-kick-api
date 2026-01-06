using Common.Abstractions;

namespace Habits.Domain.Entities;

public class OutboxMessage : IAuditable
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool Published { get; set; }
    public DateTime? PublishedAt { get; set; }
}