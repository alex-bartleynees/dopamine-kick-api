using Notifications.Domain.Entities;

namespace Notifications.Application.Abstractions;

public interface INotificationsRepository
{
    void CreateAsync(WebPushSubscription subscription, CancellationToken ct = default);
    Task<WebPushSubscription?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<WebPushSubscription?> GetByUserIdAndEndpointAsync(Guid userId, string endpoint, CancellationToken ct = default);
    void Delete(WebPushSubscription subscription);
}
