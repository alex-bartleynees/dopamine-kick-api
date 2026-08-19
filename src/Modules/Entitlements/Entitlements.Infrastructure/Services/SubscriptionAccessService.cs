using Entitlements.Domain;
using Entitlements.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Entitlements.Infrastructure.Services;

public class SubscriptionAccessService(EntitlementsContext context) : ISubscriptionAccessService
{
    public Task<bool> HasActiveAccessAsync(Guid userId, CancellationToken ct = default) =>
        context.Entitlements
            .AsNoTracking()
            .AnyAsync(
                e => e.ProductId == EntitlementProducts.DopamineKick
                    && e.UserId == userId
                    && e.HasAccess,
                ct);
}
