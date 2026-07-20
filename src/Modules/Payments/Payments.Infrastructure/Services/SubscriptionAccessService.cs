using Microsoft.EntityFrameworkCore;
using Payments.Domain.Billing;
using Payments.Infrastructure.DbContexts;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Payments-module implementation of the shared <see cref="ISubscriptionAccessService"/>. Resolves
/// the user's subscription via the customer mapping and answers the entitlement question so the Host
/// can gate premium features without referencing the Payments module directly.
/// </summary>
public class SubscriptionAccessService(PaymentsContext context) : ISubscriptionAccessService
{
    public async Task<bool> HasActiveAccessAsync(Guid userId, CancellationToken ct = default)
    {
        var status = await (
            from mapping in context.CustomerMappings.AsNoTracking()
            join state in context.SubscriptionStates.AsNoTracking()
                on mapping.CustomerReference equals state.CustomerReference
            where mapping.UserId == userId
            select state.Status).FirstOrDefaultAsync(ct);

        // No matching row projects the enum default (Unknown), which does not grant access.
        return status.GrantsAccess();
    }
}
