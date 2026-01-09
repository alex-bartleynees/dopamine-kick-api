namespace Notifications.Application.Abstractions;

public interface IWebPushService
{
    Task SendNotificationToUserAsync(Guid userId, string title, string body, string? icon, object? data, CancellationToken ct);
}
