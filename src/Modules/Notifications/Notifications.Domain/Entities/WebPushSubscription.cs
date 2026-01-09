using Common.Abstractions;

namespace Notifications.Domain.Entities;

public class WebPushSubscription : IAuditable 
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string P256dh { get; set; } = string.Empty;

    public string Auth { get; set; } = string.Empty;
    
    public DateTimeOffset? LastNotificationSentAt { get; set; }
    
    public int FailureCount { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}