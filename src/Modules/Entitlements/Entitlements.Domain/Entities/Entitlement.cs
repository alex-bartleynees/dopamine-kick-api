using SharedKernel.Abstractions;

namespace Entitlements.Domain.Entities;

/// <summary>
/// Local read-model of the payment gateway's entitlement decision for a (product, user) pair.
/// Populated by <c>EntitlementConsumerService</c> consuming <c>SubscriptionEntitlementChanged</c>
/// events; never written by the monolith directly.
/// </summary>
public class Entitlement : IAuditable
{
    public string ProductId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public bool HasAccess { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
