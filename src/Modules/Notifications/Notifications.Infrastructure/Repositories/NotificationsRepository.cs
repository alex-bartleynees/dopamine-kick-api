using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.DbContexts;

namespace Notifications.Infrastructure.Repositories;

public class NotificationsRepository(NotificationsContext context) : INotificationsRepository
{
    public void CreateAsync(WebPushSubscription subscription, CancellationToken ct = default)
    {
        context.WebPushSubscriptions.Add(subscription);
    }

    public async Task<WebPushSubscription?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await context.WebPushSubscriptions
            .Where(s => s.Id == id && s.UserId == userId)
            .FirstOrDefaultAsync(ct);
    }

    public void Delete(WebPushSubscription subscription)
    {
        context.WebPushSubscriptions.Remove(subscription);
    }
}
