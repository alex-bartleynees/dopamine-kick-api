using SharedKernel.Messaging.Abstractions;

namespace Common.IntegrationEvents.Payments;

/// <summary>
/// Published by the payment gateway whenever a customer's subscription is (re)synced. Consuming
/// products update a local read-model and gate locally without ever calling the gateway or reading its
/// database. <see cref="HasAccess"/> is the gateway's authoritative access decision.
/// </summary>
[IntegrationEventRoutingKey(RoutingKeys.SubscriptionEntitlementChangedKey)]
public record SubscriptionEntitlementChanged(
    Guid MessageId,
    string ProductId,
    Guid UserId,
    string Status,
    bool HasAccess,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd) : IntegrationEvent;
