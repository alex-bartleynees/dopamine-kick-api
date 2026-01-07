using Common.Abstractions;

namespace Notifications.Domain.Entities;

public class ProcessedMessage : IAuditable
{
    public Guid MessageId { get; set; }
    
    public string MessageType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}